using MediatR;

namespace nin.Application.Common.Interfaces;

// ReSharper disable once TypeParameterCanBeVariant
public interface IQuery<TResponse> : IRequest<TResponse>{}