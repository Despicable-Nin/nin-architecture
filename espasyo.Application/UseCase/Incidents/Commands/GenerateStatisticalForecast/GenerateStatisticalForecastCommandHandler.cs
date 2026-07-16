using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.GenerateStatisticalForecast;

public class GenerateStatisticalForecastCommandHandler(
    IMachineLearningService machineLearningService,
    ISpatialForecastService spatialForecastService,
    ISeasonalForecastService seasonalForecastService,
    ILogger<GenerateStatisticalForecastCommandHandler> logger
) : IRequestHandler<GenerateStatisticalForecastCommand, ForecastResponse>
{
    public async Task<ForecastResponse> Handle(GenerateStatisticalForecastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing statistical forecast request with {ClusterCount} clusters, horizon: {Horizon}, model: {ModelType}", 
                request.ClusterData?.Count() ?? 0, request.Horizon, request.ModelType);

            if (request.ClusterData == null || !request.ClusterData.Any())
                throw new ArgumentException("Cluster data is required for forecasting");

            if (request.Horizon < 6 || request.Horizon > 24)
                throw new ArgumentException("Forecast horizon must be between 6 and 24 months");

            if (request.ConfidenceLevel <= 0 || request.ConfidenceLevel >= 1)
                throw new ArgumentException("Confidence level must be between 0 and 1");

            var parameters = new ForecastParameters
            {
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
                CustomThresholds = request.CustomThresholds,
                RiskScoringConfig = request.RiskScoringConfig
            };

            // Temporal (always runs — base forecast)
            var forecast = await machineLearningService.GenerateStatisticalForecast(request.ClusterData, parameters);

            var flatForecasts = FlattenTemporalSeries(forecast.Series, parameters.IncludeTimeOfDay ? request.ClusterData : null);

            // Spatial
            var spatialRows = await spatialForecastService.DistributeForecast(
                request.ClusterData, parameters, forecast.Series);

            // Seasonal decomposition
            var seasonalPredictions = await seasonalForecastService.PredictSeasonal(request.ClusterData, parameters);

            forecast = forecast with
            {
                Forecasts = flatForecasts,
                Spatial = spatialRows ?? [],
                SeasonalPredictions = seasonalPredictions ?? []
            };

            var enhancedForecast = EnhanceForecastWithExplanations(forecast, request, parameters);

            logger.LogInformation("Successfully generated forecast — temporal: {TempCount}, spatial: {SpatialCount}, seasonal: {SeasonCount}",
                enhancedForecast.Forecasts.Count,
                enhancedForecast.Spatial.Count,
                enhancedForecast.SeasonalPredictions.Count);

            return enhancedForecast;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating statistical forecast");
            throw;
        }
    }

    /// <summary>
    /// Enhances forecast response with inline summaries and explanations for UI consumption
    /// </summary>
    private ForecastResponse EnhanceForecastWithExplanations(
        ForecastResponse originalForecast, 
        GenerateStatisticalForecastCommand request, 
        ForecastParameters parameters)
    {
        var allForecasts = originalForecast.Forecasts;
        
        // Generate summary
        var summary = GenerateForecastSummary(allForecasts, originalForecast);
        
        // Generate explanation based on model type
        var explanation = GenerateForecastExplanation(parameters.ModelType, originalForecast.Metrics, request.ClusterData.Count());
        
        return originalForecast with 
        {
            Summary = summary,
            Explanation = explanation
        };
    }
    
    private ForecastSummary GenerateForecastSummary(List<ForecastRow> allForecasts, ForecastResponse forecast)
    {
        if (!allForecasts.Any())
        {
            return new ForecastSummary
            {
                TotalForecasts = 0,
                HighRiskPredictions = 0,
                CriticalRiskPredictions = 0,
                OverallTrend = "stable",
                DominantRiskLevel = "low",
                AverageConfidence = 0,
                KeyInsight = "Not enough historical data per precinct/crime type to generate reliable forecasts. At least 12 months of data per combination is required.",
                RecommendedActions = ["Collect more incident data before running forecasts", "Ensure incidents are recorded consistently across all precincts"]
            };
        }

        var highRiskCount = allForecasts.Count(f => f.RiskLevel == "high");
        var criticalRiskCount = allForecasts.Count(f => f.RiskLevel == "critical");
        var increasingTrends = allForecasts.Count(f => f.Trend == "increasing");
        var decreasingTrends = allForecasts.Count(f => f.Trend == "decreasing");
        
        var overallTrend = increasingTrends > decreasingTrends ? "increasing" : 
                          decreasingTrends > increasingTrends ? "decreasing" : "stable";
        
        var dominantRisk = criticalRiskCount > 0 ? "critical" :
                          highRiskCount > allForecasts.Count / 2 ? "high" : "medium";
        
        var keyInsight = GenerateKeyInsight(overallTrend, dominantRisk, highRiskCount + criticalRiskCount);
        var actions = GenerateRecommendedActions(dominantRisk, overallTrend);

        return new ForecastSummary
        {
            TotalForecasts = allForecasts.Count,
            HighRiskPredictions = highRiskCount,
            CriticalRiskPredictions = criticalRiskCount,
            OverallTrend = overallTrend,
            DominantRiskLevel = dominantRisk,
            AverageConfidence = allForecasts.Average(f => f.Confidence),
            KeyInsight = keyInsight,
            RecommendedActions = actions,
            AverageCompositeRiskScore = 0,
            MaxCompositeRiskScore = 0
        };
    }
    
    private ForecastExplanation GenerateForecastExplanation(string modelType, ForecastMetrics metrics, int dataPoints)
    {
        var modelDesc = modelType.ToLower() switch
        {
            "ssa" => "Singular Spectrum Analysis (SSA) uses advanced mathematical decomposition to identify patterns and trends in historical crime data, providing sophisticated forecasts with confidence intervals.",
            "linear" => "Linear forecasting analyzes recent trends to project future patterns based on straight-line progression, suitable for short-term predictions with clear directional trends.",
            "seasonal" => "Seasonal forecasting combines trend analysis with monthly pattern recognition to account for recurring crime patterns throughout the year.",
            _ => "Statistical forecasting model analyzes historical patterns to predict future crime incidents."
        };
        
        var confidenceExp = $"Confidence levels indicate prediction reliability. {metrics.ModelAccuracy:P0} accuracy means the model correctly predicts trends in {metrics.ModelAccuracy:P0} of cases based on validation testing.";
        
        var trendAnalysis = "Trends show directional changes: 'Increasing' suggests rising crime rates, 'Decreasing' indicates declining rates, 'Stable' means consistent patterns.";
        
        var riskLogic = "Risk levels are calculated relative to historical averages: Low (<80% of average), Medium (80-120%), High (120-150%), Critical (>150% of historical average).";
        
        var limitations = dataPoints < 24 ? 
            "Limited historical data may reduce forecast accuracy. Recommendations include collecting more data for improved predictions." :
            "Forecasts are statistical projections based on historical patterns and may not account for unprecedented events or policy changes.";
        
        var interpretation = "Use forecasts for resource planning and trend monitoring. Focus on directional trends rather than exact numbers. Combine with local knowledge and recent intelligence for operational decisions.";
        
        return new ForecastExplanation
        {
            ModelDescription = modelDesc,
            DataQualityNotes = $"Analysis based on {dataPoints} historical data points. Model accuracy: {metrics.ModelAccuracy:P1}.",
            ConfidenceExplanation = confidenceExp,
            TrendAnalysis = trendAnalysis,
            RiskAssessmentLogic = riskLogic,
            LimitationsAndCaveats = limitations,
            HowToInterpret = interpretation
        };
    }
    
    private string GenerateKeyInsight(string trend, string riskLevel, int highRiskCount)
    {
        return trend switch
        {
            "increasing" when riskLevel == "critical" => $"Alert: Forecasts show increasing crime trends with {highRiskCount} high-risk predictions requiring immediate attention.",
            "increasing" => $"Upward crime trend detected with {highRiskCount} elevated risk periods. Enhanced monitoring recommended.",
            "decreasing" => "Positive trend: Crime rates show declining pattern. Maintain current strategies while monitoring for changes.",
            _ => highRiskCount > 0 ? $"Stable overall trend with {highRiskCount} periods requiring increased vigilance." : "Stable crime patterns with manageable risk levels forecasted."
        };
    }
    
    private List<string> GenerateRecommendedActions(string riskLevel, string trend)
    {
        var actions = new List<string>();
        
        if (riskLevel == "critical")
        {
            actions.Add("Deploy additional patrol units during high-risk periods");
            actions.Add("Coordinate with specialized units for enhanced response");
            actions.Add("Implement proactive crime prevention measures");
        }
        else if (riskLevel == "high")
        {
            actions.Add("Increase patrol frequency in affected areas");
            actions.Add("Enhance community engagement and visibility");
        }
        
        if (trend == "increasing")
        {
            actions.Add("Review and adjust resource allocation strategies");
            actions.Add("Analyze contributing factors for trend reversal");
        }
        
        if (!actions.Any())
        {
            actions.Add("Continue regular monitoring and assessment");
            actions.Add("Maintain current patrol and response protocols");
        }
        
        return actions;
    }

    private record TimeOfDayProps(double Morning, double Afternoon, double Evening);

    private static Dictionary<(int Precinct, int CrimeType), TimeOfDayProps> ComputeTimeOfDayProportions(
        IEnumerable<ClusterGroup> clusterData)
    {
        var items = clusterData.SelectMany(g => g.ClusterItems).ToList();
        var groups = items
            .GroupBy(i => ((int)i.Precinct, (int)i.CrimeType))
            .ToList();

        var result = new Dictionary<(int, int), TimeOfDayProps>();
        foreach (var group in groups)
        {
            var total = group.Count();
            if (total == 0) continue;
            var morning = group.Count(i => i.TimeOfDay == "Morning") / (double)total;
            var afternoon = group.Count(i => i.TimeOfDay == "Afternoon") / (double)total;
            var evening = group.Count(i => i.TimeOfDay == "Evening") / (double)total;

            if (morning == 0 && afternoon == 0 && evening == 0)
            {
                result[group.Key] = new TimeOfDayProps(1.0 / 3, 1.0 / 3, 1.0 / 3);
            }
            else
            {
                result[group.Key] = new TimeOfDayProps(morning, afternoon, evening);
            }
        }
        return result;
    }

    private static List<ForecastRow> FlattenTemporalSeries(List<ForecastSeries> series, IEnumerable<ClusterGroup>? clusterDataForTimeOfDay)
    {
        var timeOfDayProportions = clusterDataForTimeOfDay != null
            ? ComputeTimeOfDayProportions(clusterDataForTimeOfDay)
            : null;

        return series.SelectMany(s =>
        {
            var key = (s.Precinct, s.CrimeType);
            if (timeOfDayProportions != null && timeOfDayProportions.TryGetValue(key, out var props))
            {
                return s.Forecasts.SelectMany(f => new[]
                {
                    new ForecastRow
                    {
                        PredictionType = "temporal",
                        Precinct = s.Precinct,
                        CrimeType = s.CrimeType,
                        ClusterId = s.ClusterId,
                        Timestamp = f.Timestamp,
                        Forecast = f.Forecast * props.Morning,
                        LowerBound = f.LowerBound * props.Morning,
                        UpperBound = f.UpperBound * props.Morning,
                        Confidence = f.Confidence,
                        Trend = f.Trend,
                        RiskLevel = f.RiskLevel,
                        TimeOfDay = "Morning"
                    },
                    new ForecastRow
                    {
                        PredictionType = "temporal",
                        Precinct = s.Precinct,
                        CrimeType = s.CrimeType,
                        ClusterId = s.ClusterId,
                        Timestamp = f.Timestamp,
                        Forecast = f.Forecast * props.Afternoon,
                        LowerBound = f.LowerBound * props.Afternoon,
                        UpperBound = f.UpperBound * props.Afternoon,
                        Confidence = f.Confidence,
                        Trend = f.Trend,
                        RiskLevel = f.RiskLevel,
                        TimeOfDay = "Afternoon"
                    },
                    new ForecastRow
                    {
                        PredictionType = "temporal",
                        Precinct = s.Precinct,
                        CrimeType = s.CrimeType,
                        ClusterId = s.ClusterId,
                        Timestamp = f.Timestamp,
                        Forecast = f.Forecast * props.Evening,
                        LowerBound = f.LowerBound * props.Evening,
                        UpperBound = f.UpperBound * props.Evening,
                        Confidence = f.Confidence,
                        Trend = f.Trend,
                        RiskLevel = f.RiskLevel,
                        TimeOfDay = "Evening"
                    }
                });
            }

            return s.Forecasts.Select(f => new ForecastRow
            {
                PredictionType = "temporal",
                Precinct = s.Precinct,
                CrimeType = s.CrimeType,
                ClusterId = s.ClusterId,
                Timestamp = f.Timestamp,
                Forecast = f.Forecast,
                LowerBound = f.LowerBound,
                UpperBound = f.UpperBound,
                Confidence = f.Confidence,
                Trend = f.Trend,
                RiskLevel = f.RiskLevel,
                TimeOfDay = null
            });
        }).ToList();
    }
}
