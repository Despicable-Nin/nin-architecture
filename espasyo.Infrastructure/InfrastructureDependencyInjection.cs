using espasyo.Application.Interfaces;
using espasyo.Infrastructure.Data;
using espasyo.Infrastructure.Data.Interceptors;
using espasyo.Infrastructure.Data.Repositories;
using espasyo.Infrastructure.Geocoding;
using espasyo.Infrastructure.MachineLearning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.ML;

namespace espasyo.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["DatabaseProvider"] ?? "SqlServer";

        switch (databaseProvider.ToLower())
        {
            case "sqlite":
                services.AddSqliteInfrastructure(configuration);
                break;
            case "sqlserver":
            default:
                services.AddSqlServerInfrastructure(configuration);
                break;
        }

        return services;
    }

    private static IServiceCollection AddSqlServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetRequiredService<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });
        
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<MLContext>();
        
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IStreetRepository, StreetRepository>();
        services.AddTransient<IGeocodeService, AddressGeocodeService>();
        services.AddTransient<IMachineLearningService, MachineLearningService>();

        services.AddHttpClient<AddressGeocodeService>();

        return services;
    }
}
