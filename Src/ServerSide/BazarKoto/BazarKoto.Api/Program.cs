using BazarKoto.Application.Interfaces;
using BazarKoto.Api.Extensions;
using BazarKoto.Api.Middleware;
using BazarKoto.Infrastructure.Persistence;
using BazarKoto.Infrastructure.Persistence.Seed;
using BazarKoto.Infrastructure.Persistence.Seed.MasterData;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/bazarkoto-api-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BazarKotoDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await dbContext.Database.MigrateAsync();

        try
        {
            await SeedData.SeedAsync(dbContext, app.Configuration, passwordHasher, app.Environment.IsDevelopment());
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Normal seed data did not run successfully. Master data seed will still be attempted in Development.");
        }

        if (app.Environment.IsDevelopment())
        {
            var masterDataSeeder = scope.ServiceProvider.GetRequiredService<MasterDataSeeder>();
            var outputMasterDataPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seed", "MasterData");
            var projectMasterDataPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "BazarKoto.Infrastructure", "Persistence", "Seed", "MasterData"));
            var masterDataPath = Directory.Exists(outputMasterDataPath) ? outputMasterDataPath : projectMasterDataPath;
            logger.LogInformation("Master data seed path: {MasterDataPath}", masterDataPath);
            await masterDataSeeder.SeedAsync(masterDataPath);
        }
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Seed data did not run successfully.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestTrackingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
