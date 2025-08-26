using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.GenerateStatisticalForecast;

public class GenerateStatisticalForecastCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<GenerateStatisticalForecastCommandHandler> logger
) : IRequestHandler<GenerateStatisticalForecastCommand, ForecastResponse>
{
    public async Task<ForecastResponse> Handle(GenerateStatisticalForecastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing statistical forecast request with {ClusterCount} clusters, horizon: {Horizon}, model: {ModelType}", 
                request.ClusterData?.Count() ?? 0, request.Horizon, request.ModelType);

            // Validate input
            if (request.ClusterData == null || !request.ClusterData.Any())
            {
                throw new ArgumentException("Cluster data is required for forecasting");
            }

            if (request.Horizon < 1 || request.Horizon > 24)
            {
                throw new ArgumentException("Forecast horizon must be between 1 and 24 months");
            }

            if (request.ConfidenceLevel <= 0 || request.ConfidenceLevel >= 1)
            {
                throw new ArgumentException("Confidence level must be between 0 and 1");
            }

            // Create forecast parameters
            var parameters = new ForecastParameters
            {
                Horizon = request.Horizon,
                ConfidenceLevel = request.ConfidenceLevel,
                ModelType = request.ModelType,
                IncludeSeasonality = request.IncludeSeasonality,
                WeightRecentData = request.WeightRecentData
            };

            // Generate statistical forecast using ML service
            var forecast = await machineLearningService.GenerateStatisticalForecast(request.ClusterData, parameters);

            logger.LogInformation("Successfully generated statistical forecast with {SeriesCount} forecast series", 
                forecast.Series.Count);

            return forecast;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating statistical forecast");
            throw;
        }
    }
}
