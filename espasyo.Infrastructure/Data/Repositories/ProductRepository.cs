using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace espasyo.Infrastructure.Data.Repositories;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await context.Products.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await context.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
    }
}