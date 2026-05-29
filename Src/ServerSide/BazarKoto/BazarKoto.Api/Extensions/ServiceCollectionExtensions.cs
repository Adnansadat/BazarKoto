using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using BazarKoto.Api.Filters;
using BazarKoto.Api.Services;
using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Application.Validators;
using BazarKoto.Contracts.Common;
using BazarKoto.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace BazarKoto.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidateModelAttribute>())
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid request value." : x.ErrorMessage)
                        .ToList();

                    return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation failed.", errors));
                };
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BazarKoto API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token like this: Bearer YOUR_ACCESS_TOKEN"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document, null),
                    []
                }
            });
        });
        services.AddApplicationServices();
        services.AddInfrastructure(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserTrackingRequestContextAccessor, HttpUserTrackingRequestContextAccessor>();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();
        services.AddCorsPolicy(configuration);
        services.AddApiRateLimiting();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPriceService, PriceService>();
        services.AddScoped<IPriceSummaryService, PriceSummaryService>();
        services.AddScoped<IUserTrackingService, UserTrackingService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IContactService, ContactService>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"] ?? "development-only-secret-key-change-me-development-only";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = key,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("AngularFrontend", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var key = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
