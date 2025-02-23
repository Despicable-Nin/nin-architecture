﻿using espasyo.Application.Common.Interfaces;
using espasyo.Application.Interfaces;
using espasyo.Infrastructure.Data;
using espasyo.Infrastructure.Data.Interceptors;
using espasyo.Infrastructure.Data.Repositories;
using espasyo.Infrastructure.Geocoding;
using espasyo.Infrastructure.MachineLearning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.ML;

namespace espasyo.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("No connection string");

        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetService<ISaveChangesInterceptor>() ?? throw new InvalidOperationException("ISaveChangesInterceptor is not implemented."));
            options.UseSqlServer(connectionString);
        });

        services.AddSingleton<MLContext>();
        
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddTransient<IGeocodeService, AddressGeocodeService>();
        services.AddTransient<IMachineLearningService, MachineLearningService>();

        services.AddHttpClient<AddressGeocodeService>();

        return services;
    }
}