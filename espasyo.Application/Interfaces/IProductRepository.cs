using espasyo.Domain.Entities;

namespace espasyo.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
}