using espasyo.Application.Behaviors;
using espasyo.Application.Products.Queries.GetProducts;
using MediatR;

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
}
