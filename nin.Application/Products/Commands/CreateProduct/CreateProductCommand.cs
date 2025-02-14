﻿using MediatR;
using nin.Application.Common.Interfaces;
using nin.Domain.Entities;

namespace nin.Application.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<Unit>
{
    public string? Name { get; init;}
    public decimal Price { get; init; }
}

public class CreateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, Unit>
{

    public async Task<Unit> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await productRepository.AddAsync(new Product(request.Name, request.Price));
        return Unit.Value;
    }
}