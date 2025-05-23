﻿using MediatR;

namespace espasyo.Application.Products.Queries.GetProducts;


public record GetProductsQuery() : IRequest<IEnumerable<ProductResult>>;

public record ProductResult
{
    public Guid Id { get; init; }
    public string Name { get; init;}
    public decimal Price { get; init; }
    public string? Description => nameof(ProductResult);
}