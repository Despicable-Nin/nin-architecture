using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;

public class SaveForecastSnapshotCommandHandler(
    IForecastRepository forecastRepository,
    IPrecinctRepository precinctRepository
) : IRequestHandler<SaveForecastSnapshotCommand, SaveForecastSnapshotResponse>
{
    public async Task<SaveForecastSnapshotResponse> Handle(SaveForecastSnapshotCommand request, CancellationToken cancellationToken)
    {
        var precinctId = Guid.Empty;
        if (request.Predictions.Count > 0)
        {
            var precinct = await precinctRepository.GetByBarangayAsync((Barangay)request.Predictions[0].Precinct);
            if (precinct != null)
                precinctId = precinct.Id;
        }

        var run = new ForecastRun(
            precinctId,
            request.ForecastPeriod,
            request.ConfidenceLevel,
            ForecastModelTypeEnum.SSA,
            request.GeneratedById,
            request.Name);

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
