using MediatR;

namespace nin.Application.Common.Interfaces;

public interface IQuery<TResponse> : IRequest<TResponse>{}