using espasyo.Application.Common.Interfaces;
using MediatR;

namespace espasyo.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, IEnumerable<ProductResult>>
{
    public async Task<IEnumerable<ProductResult>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // return await Task.FromResult(new List<Product>
        // {
        //     new Product("Laptop", 1200),
        //     new Product("Mouse", 25)
        // });

        var list = await productRepository.GetAllProductsAsync();
        return list.Select(a => new ProductResult
        {
            Id = a.Id,
            Name = a.Name,
            Price = a.Price,
        }).ToArray();
    }
}