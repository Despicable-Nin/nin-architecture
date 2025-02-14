using MediatR;
using nin.Domain.Entities;

namespace nin.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<Product>>
{
    public async Task<List<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new List<Product>
        {
            new Product("Laptop", 1200),
            new Product("Mouse", 25)
        });
    }
}