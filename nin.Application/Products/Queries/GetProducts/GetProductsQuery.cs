using nin.Application.Common.Interfaces;
using nin.Domain.Entities;

namespace nin.Application.Products.Queries.GetProducts;


public record GetProductsQuery() : IQuery<List<Product>>;