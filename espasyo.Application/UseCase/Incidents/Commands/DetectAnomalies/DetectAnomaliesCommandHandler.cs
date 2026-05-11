using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace espasyo.Application.UseCase.Incidents.Commands.DetectAnomalies;

public class DetectAnomaliesCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<DetectAnomaliesCommandHandler> logger
) : IRequestHandler<DetectAnomaliesCommand, AnomalyDetectionResponse>
{
    public async Task<AnomalyDetectionResponse> Handle(DetectAnomaliesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Detecting anomalies with method: {Method}, groupBy: {GroupBy}, clusters: {ClusterCount}",
                request.Method, request.GroupBy, request.ClusterData?.Count() ?? 0);

            if (request.ClusterData == null || !request.ClusterData.Any())
                throw new ArgumentException("Cluster data is required for anomaly detection");

            var anomalyRequest = new AnomalyDetectionRequest
            {
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                Method = request.Method,
                GroupBy = request.GroupBy
            };

            var anomalies = await machineLearningService.DetectAnomaliesAsync(request.ClusterData, anomalyRequest);

            var totalDataPoints = request.ClusterData.Sum(g => g.ClusterItems.Count);
            var avgMonthlyCount = totalDataPoints > 0
                ? (double)totalDataPoints / (request.ClusterData.Max(g => g.ClusterItems.Max(i => i.Year * 12 + i.Month)) -
                                             request.ClusterData.Min(g => g.ClusterItems.Min(i => i.Year * 12 + i.Month)) + 1)
                : 0;

            return new AnomalyDetectionResponse
            {
                Anomalies = anomalies,
                TotalAnomalies = anomalies.Count,
                TotalDataPoints = totalDataPoints,
                AnomalyRate = totalDataPoints > 0 ? (double)anomalies.Count / totalDataPoints * 100 : 0
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error detecting anomalies");
            throw;
        }
    }
}
