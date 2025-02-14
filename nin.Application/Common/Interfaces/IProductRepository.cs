using nin.Domain.Entities;

namespace nin.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
}