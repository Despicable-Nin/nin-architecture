using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.ValidateForecastModel;

public class ValidateForecastModelCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<ValidateForecastModelCommandHandler> logger
) : IRequestHandler<ValidateForecastModelCommand, ForecastValidationResult>
{
    public async Task<ForecastValidationResult> Handle(ValidateForecastModelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing forecast validation request with {ClusterCount} clusters, model: {ModelType}", 
                request.ClusterData?.Count() ?? 0, request.ModelType);

            // Validate input
            if (request.ClusterData == null || !request.ClusterData.Any())
            {
                throw new ArgumentException("Cluster data is required for model validation");
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

            // Validate forecast model using ML service
            var validation = await machineLearningService.ValidateForecastModel(request.ClusterData, parameters);

            logger.LogInformation("Forecast model validation completed. Reliable: {IsReliable}, MAPE: {MAPE}%", 
                validation.IsReliable, validation.Metrics.MeanAbsolutePercentageError);

            return validation;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating forecast model");
            throw;
        }
    }
}
