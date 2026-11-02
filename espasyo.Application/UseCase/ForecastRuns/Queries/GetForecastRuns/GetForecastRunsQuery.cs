using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastRuns;

public record GetForecastRunsQuery : IRequest<GetForecastRunsResponse>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record ForecastRunResult
{
    public Guid Id { get; init; }
    public string PrecinctName { get; init; } = string.Empty;
    public string PrecinctCode { get; init; } = string.Empty;
    public DateTimeOffset RunAt { get; init; }
    public int Horizon { get; init; }
    public string ModelType { get; init; } = string.Empty;
    public string RequestedModelType { get; init; } = string.Empty;
    public string EffectiveModelType { get; init; } = string.Empty;
    public string? ModelFallbackReason { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalSeries { get; init; }
    public string GeneratedById { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record GetForecastRunsResponse
{
    public IEnumerable<ForecastRunResult> Runs { get; init; } = [];
    public int TotalCount { get; init; }
}

public class GetForecastRunsQueryHandler(IForecastRepository forecastRepository)
    : IRequestHandler<GetForecastRunsQuery, GetForecastRunsResponse>
{
    public async Task<GetForecastRunsResponse> Handle(GetForecastRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = await forecastRepository.GetForecastRunsAsync(request.Page, request.PageSize);
        var allRuns = await forecastRepository.GetForecastRunsAsync(1, int.MaxValue);
        var totalCount = allRuns.Count();

        var results = runs.Select(r => new ForecastRunResult
        {
            Id = r.Id,
            PrecinctName = r.Precinct?.Barangay.ToString() ?? "Unknown",
            PrecinctCode = r.Precinct?.Code ?? "",
            RunAt = r.RunAt,
            Horizon = r.Horizon,
            ModelType = r.EffectiveModelType.Length > 0 ? r.EffectiveModelType : r.ModelType.ToString(),
            RequestedModelType = r.RequestedModelType,
            EffectiveModelType = r.EffectiveModelType.Length > 0 ? r.EffectiveModelType : r.ModelType.ToString(),
            ModelFallbackReason = r.ModelFallbackReason,
            Status = r.Status.ToString(),
            TotalSeries = r.TotalSeries,
            GeneratedById = r.GeneratedById,
            Name = r.Name
        });

        return new GetForecastRunsResponse
        {
            Runs = results,
            TotalCount = totalCount
        };
    }
}
