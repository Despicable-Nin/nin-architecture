using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

public class PredictHotspotsCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<PredictHotspotsCommandHandler> logger
) : IRequestHandler<PredictHotspotsCommand, HotspotPredictionResponse>
{
    public async Task<HotspotPredictionResponse> Handle(PredictHotspotsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Predicting hotspots with {ClusterCount} clusters, horizon: {Horizon}",
                request.ClusterData?.Count() ?? 0, request.Horizon);

            if (request.ClusterData == null || !request.ClusterData.Any())
                throw new ArgumentException("Cluster data is required for hotspot prediction");

            var hotspotRequest = new HotspotPredictionRequest
            {
                ClusterData = request.ClusterData,
                Horizon = request.Horizon,
                ConfidenceLevel = request.ConfidenceLevel,
                ModelType = request.ModelType,
                IncludeSeasonality = request.IncludeSeasonality,
                WeightRecentData = request.WeightRecentData,
                IncludeTimeOfDay = request.IncludeTimeOfDay,
                IncludeMonthOfYear = request.IncludeMonthOfYear,
                IncludeTrend = request.IncludeTrend,
                CrimeTypeFilter = request.CrimeTypeFilter,
                SeverityFilter = request.SeverityFilter,
                HotspotThreshold = request.HotspotThreshold
            };

            var features = await machineLearningService.PredictHotspotsAsync(request.ClusterData, hotspotRequest);

            // Generate forecast summary with recommendations using the same logic as GenerateRecommendedActions
            var summary = GenerateHotspotSummary(features, request.ClusterData);

            logger.LogInformation("Generated {FeatureCount} hotspot features", features.Features.Count);
            return new HotspotPredictionResponse
            {
                Features = features,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error predicting hotspots");
            throw;
        }
    }

    /// <summary>
    /// Generates a ForecastSummary with recommended actions based on hotspot risk level and trend,
    /// using the same methodology as GenerateRecommendedActions in the statistical forecast handler.
    /// </summary>
    private ForecastSummary GenerateHotspotSummary(GeoJsonFeatureCollection features, IEnumerable<ClusterGroup> clusterData)
    {
        // Compute dominant risk level from hotspot features
        var criticalCount = features.Features.Count(f => f.Properties?.ContainsKey("severity") == true && 
            f.Properties?.TryGetValue("severity", out var sev) == true && 
            sev.ToString() == "critical");
        var highCount = features.Features.Count(f => f.Properties?.ContainsKey("severity") == true && 
            f.Properties?.TryGetValue("severity", out var sev2) == true && 
            sev2.ToString() == "high");
        var totalFeatures = features.Features.Count;

        var dominantRiskLevel = criticalCount > 0 ? "critical" :
                                highCount > totalFeatures / 2 ? "high" : "medium";

        // Compute trend from features
        var increasingCount = features.Features.Count(f => f.Properties?.ContainsKey("trend") == true &&
            f.Properties?.TryGetValue("trend", out var trend) == true &&
            trend.ToString() == "increasing");
        var decreasingCount = features.Features.Count(f => f.Properties?.ContainsKey("trend") == true &&
            f.Properties?.TryGetValue("trend", out var trend2) == true &&
            trend2.ToString() == "decreasing");
        var overallTrend = increasingCount > decreasingCount ? "increasing" :
                           decreasingCount > increasingCount ? "decreasing" : "stable";

        // Generate recommended actions using the same pattern as GenerateRecommendedActions
        var recommendedActions = new List<string>();

        if (dominantRiskLevel == "critical")
        {
            recommendedActions.Add("Deploy additional patrol units during high-risk periods");
            recommendedActions.Add("Coordinate with specialized units for enhanced response");
            recommendedActions.Add("Implement proactive crime prevention measures");
        }
        else if (dominantRiskLevel == "high")
        {
            recommendedActions.Add("Increase patrol frequency in affected areas");
            recommendedActions.Add("Enhance community engagement and visibility");
        }

        if (increasingCount > 0)
        {
            recommendedActions.Add("Review and adjust resource allocation strategies");
            recommendedActions.Add("Analyze contributing factors for trend reversal");
        }

        if (!recommendedActions.Any())
        {
            recommendedActions.Add("Continue regular monitoring and assessment");
            recommendedActions.Add("Maintain current patrol and response protocols");
        }

        // Compute key metrics
        var totalPredicted = features.Features.Sum(f =>
        {
            if (f.Properties?.ContainsKey("totalPredicted") == true &&
                f.Properties.TryGetValue("totalPredicted", out var tp) == true &&
                tp is double total)
            {
                return total;
            }
            return 0;
        });

        var avgConfidence = features.Features.Average(f =>
        {
            if (f.Properties?.ContainsKey("confidence") == true &&
                f.Properties.TryGetValue("confidence", out var cf) == true &&
                cf is double confidence)
            {
                return confidence;
            }
            return 0;
        });

        return new ForecastSummary
        {
            TotalForecasts = totalFeatures,
            HighRiskPredictions = highCount,
            CriticalRiskPredictions = criticalCount,
            OverallTrend = overallTrend,
            DominantRiskLevel = dominantRiskLevel,
            AverageConfidence = avgConfidence,
            KeyInsight = $"Hotspot analysis identified {totalFeatures} high-risk areas with {dominantRiskLevel} risk level. {(overallTrend == "increasing" ? "Trend is increasing" : overallTrend == "decreasing" ? "Trend is decreasing" : "Trend is stable")}.",
            RecommendedActions = recommendedActions,
            AverageCompositeRiskScore = 0,
            MaxCompositeRiskScore = 0
        };
    }
}
