using espasyo.Application.Common.Interfaces;
using espasyo.Infrastructure.Data;
using espasyo.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure;

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