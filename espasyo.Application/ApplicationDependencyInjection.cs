using espasyo.Application.Products.Queries.GetProducts;
using MediatR;

namespace espasyo.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));
        services.AddTransient<IRequestHandler<GetProductsQuery, IEnumerable<ProductResult>>, GetProductsQueryHandler>();
        return services;
    }
}
