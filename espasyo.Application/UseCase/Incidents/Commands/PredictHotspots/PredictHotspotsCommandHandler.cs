using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

public class PredictHotspotsCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<PredictHotspotsCommandHandler> logger
) : IRequestHandler<PredictHotspotsCommand, GeoJsonFeatureCollection>
{
    public async Task<GeoJsonFeatureCollection> Handle(PredictHotspotsCommand request, CancellationToken cancellationToken)
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
                HotspotThreshold = request.HotspotThreshold
            };

            var result = await machineLearningService.PredictHotspotsAsync(request.ClusterData, hotspotRequest);

            logger.LogInformation("Generated {FeatureCount} hotspot features", result.Features.Count);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error predicting hotspots");
            throw;
        }
    }
}
