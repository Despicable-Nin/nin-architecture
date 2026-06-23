using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;

public class SaveForecastSnapshotCommandHandler(
    IForecastRepository forecastRepository
) : IRequestHandler<SaveForecastSnapshotCommand, SaveForecastSnapshotResponse>
{
    public async Task<SaveForecastSnapshotResponse> Handle(SaveForecastSnapshotCommand request, CancellationToken cancellationToken)
    {
        var run = new ForecastRun(
            Guid.Empty,
            request.ForecastPeriod,
            request.ConfidenceLevel,
            ForecastModelTypeEnum.SSA,
            request.GeneratedById);

        run.MarkCompleted(request.Predictions.Count);

        var results = request.Predictions.Select(p => new ForecastResult(
            run.Id,
            (Barangay)p.Precinct,
            (CrimeTypeEnum)p.CrimeType,
            p.Month,
            p.Year,
            p.PredictedValue,
            p.LowerBound,
            p.UpperBound,
            p.Confidence,
            p.RiskLevel,
            p.Trend)).ToList();

        await forecastRepository.SaveForecastRunAsync(run);
        await forecastRepository.SaveForecastResultsAsync(results);

        return new SaveForecastSnapshotResponse
        {
            Id = run.Id.ToString(),
            Name = request.Name,
            CreatedAt = run.RunAt.UtcDateTime,
            TotalPredictions = results.Count
        };
    }
}
