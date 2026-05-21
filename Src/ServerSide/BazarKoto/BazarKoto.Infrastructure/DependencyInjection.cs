using BazarKoto.Application.Interfaces;
using BazarKoto.Infrastructure.Analytics;
using BazarKoto.Infrastructure.Identity;
using BazarKoto.Infrastructure.Persistence;
using BazarKoto.Infrastructure.Persistence.Seed.MasterData;
using BazarKoto.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BazarKoto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BazarKotoDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BazarKotoDbContext>());
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IPriceSummaryRepository, PriceSummaryRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<MasterDataSeeder>();
        services.AddScoped<TrafficTracker>();
        services.AddScoped<PeakHourCalculator>();

        return services;
    }
}
