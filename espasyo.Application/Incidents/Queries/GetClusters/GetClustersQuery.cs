using espasyo.Application.Common;
using espasyo.Application.Common.Interfaces;
using espasyo.Application.Common.Models.ML;
using FluentValidation;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetClusters;

public record GetClustersQuery : IRequest<GetClustersResult>
{
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
}

public record GetClustersResult
{
    public IEnumerable<ClusteredModel>? Data { get; init; } = [];
}

public class GetClustersQueryValidator : AbstractValidator<GetClustersQuery>
{
    public GetClustersQueryValidator()
    {
        RuleFor(x => x.DateFrom)
            .Must((model, dateFrom) => dateFrom <= model.DateTo)
            .WithMessage("DateFrom must be greater than DateTo");

        RuleFor(x => x.DateTo).NotEmpty().NotNull();
    }
}

public class GetClustersQueryHandler(ILogger<GetClustersQueryHandler> logger,
    IMachineLearningService kmeansService,
    IIncidentRepository repository) : IRequestHandler<GetClustersQuery, GetClustersResult>
{
    
    public async Task<GetClustersResult> Handle(GetClustersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching incident records...");
        
        var incidents = await repository.GetAllIncidentsAsync()
    }
}