using espasyo.Application.Interfaces;
using espasyo.Infrastructure.Data;
using espasyo.Infrastructure.Data.Interceptors;
using espasyo.Infrastructure.Data.Repositories.Sqlite;
using espasyo.Infrastructure.Geocoding;
using espasyo.Infrastructure.MachineLearning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.ML;

namespace espasyo.Infrastructure;

public static class SqliteInfrastructureDependencyInjection
{
    public static IServiceCollection AddSqliteInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqliteConnection") 
                               ?? throw new InvalidOperationException("SqliteConnection connection string is missing.");

        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
        services.AddDbContext<SqliteApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetRequiredService<ISaveChangesInterceptor>());
            options.UseSqlite(connectionString);
        });
        
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<SqliteApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<MLContext>();
        
        // Register SQLite-specific repositories
        services.AddScoped<IProductRepository, SqliteProductRepository>();
        services.AddScoped<IIncidentRepository, SqliteIncidentRepository>();
        services.AddScoped<IStreetRepository, SqliteStreetRepository>();
        
        // These services don't depend on database provider, so they remain the same
        services.AddTransient<IGeocodeService, AddressGeocodeService>();
        services.AddTransient<IMachineLearningService, MachineLearningService>();

        services.AddHttpClient<AddressGeocodeService>();

        return services;
    }
}
