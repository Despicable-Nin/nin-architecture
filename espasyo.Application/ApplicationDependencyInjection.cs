using espasyo.Application.Behaviors;
using espasyo.Application.Configuration;
using espasyo.Application.Products.Queries.GetProducts;
using espasyo.Application.Services;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace espasyo.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));
        services.AddTransient<IRequestHandler<GetProductsQuery, IEnumerable<ProductResult>>, GetProductsQueryHandler>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
    
    /// <summary>
    /// Adds ML-related services with configuration support
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMLServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure ML settings from appsettings.json
        services.Configure<MLSettings>(configuration.GetSection("MLSettings"));
        
        // Add ML services
        services.AddTransient<MLManpowerAllocationService>();
        
        return services;
    }
}
