using Microsoft.EntityFrameworkCore;
using nin.Application.Common.Interfaces;
using nin.Infrastructure.Data;
using nin.Infrastructure.Data.Repositories;

namespace nin.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}