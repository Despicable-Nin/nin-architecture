using espasyo.Application.Common.Models.ML;

namespace espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

/// <summary>
/// Response DTO combining hotspot prediction features with forecast summary/recommendations.
/// Used by the POST /api/incident/forecast/hotspots endpoint to deliver both spatial hotspot data
/// and risk-based prevention recommendations in a single API call.
/// </summary>
public record HotspotPredictionResponse
{
    /// <summary>
    /// GeoJSON features representing predicted crime hotspots with severity and risk levels.
    /// </summary>
    public GeoJsonFeatureCollection Features { get; init; } = new();

    /// <summary>
    /// Forecast summary including risk levels, trend, and prevention recommendations.
    /// Populated by the handler using the same logic as GenerateRecommendedActions.
    /// </summary>
    public ForecastSummary Summary { get; init; } = new();
}