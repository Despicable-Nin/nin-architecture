using MediatR;

namespace nin.Application.Common.Interfaces;

public interface ICommand<TResponse> : IRequest<TResponse>{}