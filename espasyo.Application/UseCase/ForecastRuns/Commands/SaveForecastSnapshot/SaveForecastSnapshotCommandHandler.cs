using System.Text.Json;
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
            p.Trend,
            shift: p.Shift)).ToList();
        
        

        var spatialResults = request.SpatialPredictions.Select(s => new SpatialForecastResult(
            run.Id,
            (Barangay)s.Precinct,
            s.ClusterId,
            s.Latitude,
            s.Longitude,
            s.Month,
            s.Year,
            s.PredictedValue,
            s.LowerBound,
            s.UpperBound,
            s.Confidence,
            s.RiskLevel,
            s.Trend)).ToList();

        var seasonalResults = request.SeasonalPredictions.Select(s => new SeasonalDecompositionResult(
            run.Id,
            (Barangay)s.Precinct,
            (CrimeTypeEnum)s.CrimeType,
            System.Text.Json.JsonSerializer.Serialize(s.Trend),
            System.Text.Json.JsonSerializer.Serialize(s.Seasonal),
            System.Text.Json.JsonSerializer.Serialize(s.Residual),
            s.Strength.GetValueOrDefault("Trend"),
            s.Strength.GetValueOrDefault("Seasonal"),
            s.PeakMonth,
            s.TroughMonth)).ToList();

        await forecastRepository.SaveForecastRunAsync(run);
        await forecastRepository.SaveForecastResultsAsync(results);
        if (spatialResults.Count > 0)
            await forecastRepository.SaveSpatialForecastResultsAsync(spatialResults);
        if (seasonalResults.Count > 0)
            await forecastRepository.SaveSeasonalDecompositionResultsAsync(seasonalResults);

        return new SaveForecastSnapshotResponse
        {
            Id = run.Id.ToString(),
            Name = request.Name,
            CreatedAt = run.RunAt.UtcDateTime,
            TotalPredictions = results.Count
        };
    }
}
