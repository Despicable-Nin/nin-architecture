using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;
using espasyo.Infrastructure.MachineLearning;
using Microsoft.ML;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class PredictHotspotsCommandHandlerTests
{
    [Fact]
    public async Task PredictHotspotsAsync_ProducesHotspots_ForValidClusterData()
    {
        var clusterItems = Enumerable.Range(1, 12)
            .SelectMany(month => new[]
            {
                new ClusterItem
                {
                    CaseId = $"case-{month}-1",
                    Latitude = 14.4,
                    Longitude = 121.0,
                    Month = month,
                    Day = 12,
                    Year = 2025,
                    TimeOfDay = "Morning",
                    Precinct = 0,
                    CrimeType = 0
                },
                new ClusterItem
                {
                    CaseId = $"case-{month}-2",
                    Latitude = 14.401,
                    Longitude = 121.001,
                    Month = month,
                    Day = 15,
                    Year = 2025,
                    TimeOfDay = "Afternoon",
                    Precinct = 0,
                    CrimeType = 0
                },
                new ClusterItem
                {
                    CaseId = $"case-{month}-3",
                    Latitude = 14.402,
                    Longitude = 121.002,
                    Month = month,
                    Day = 18,
                    Year = 2025,
                    TimeOfDay = "Evening",
                    Precinct = 0,
                    CrimeType = 0
                }
            })
            .ToList();
        var clusters = new[]
        {
            new ClusterGroup { ClusterId = 1, ClusterItems = clusterItems }
        };
        var loggerFactory = LoggerFactory.Create(_ => { });
        var temporalForecastService = new TemporalForecastService(
            new MLContext(seed: 1),
            loggerFactory.CreateLogger<TemporalForecastService>());
        var machineLearningService = new MachineLearningService(
            new MLContext(seed: 1),
            loggerFactory.CreateLogger<MachineLearningService>(),
            temporalForecastService);

        var response = await machineLearningService.PredictHotspotsAsync(
            clusters,
            new HotspotPredictionRequest { ModelType = "linear" });

        Assert.NotEmpty(response.Features);
    }

    [Fact]
    public async Task Handle_ReturnsNeutralSummary_WhenNoHotspotFeaturesAreGenerated()
    {
        var machineLearningService = new Mock<IMachineLearningService>();
        machineLearningService
            .Setup(service => service.PredictHotspotsAsync(
                It.IsAny<IEnumerable<ClusterGroup>>(),
                It.IsAny<HotspotPredictionRequest>()))
            .ReturnsAsync(new GeoJsonFeatureCollection());

        var handler = new PredictHotspotsCommandHandler(
            machineLearningService.Object,
            Mock.Of<ILogger<PredictHotspotsCommandHandler>>());

        var response = await handler.Handle(
            new PredictHotspotsCommand
            {
                ClusterData = [new ClusterGroup { ClusterId = 1 }]
            },
            CancellationToken.None);

        Assert.Empty(response.Features.Features);
        Assert.Equal(0, response.Summary.TotalForecasts);
        Assert.Equal(0, response.Summary.AverageConfidence);
        Assert.Equal("none", response.Summary.DominantRiskLevel);
        Assert.Contains("No forecast hotspot polygons", response.Summary.KeyInsight);
        Assert.NotEmpty(response.Summary.RecommendedActions);
    }
}
