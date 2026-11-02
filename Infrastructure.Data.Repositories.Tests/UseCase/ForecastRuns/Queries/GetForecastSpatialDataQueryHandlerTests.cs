using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastSpatialData;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.ForecastRuns.Queries;

public class GetForecastSpatialDataQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPersistedSpatialRowsForForecastRun()
    {
        var forecastRunId = Guid.NewGuid();
        var spatialResult = new SpatialForecastResult(
            forecastRunId,
            Barangay.Bayanan,
            12,
            14.3856,
            121.0189,
            12,
            2026,
            4.5,
            3,
            6,
            0.9,
            "high",
            "increasing");
        var repository = new Mock<IForecastRepository>();
        repository.Setup(repo => repo.GetSpatialForecastResultsAsync(forecastRunId))
            .ReturnsAsync([spatialResult]);

        var handler = new GetForecastSpatialDataQueryHandler(repository.Object);

        var result = (await handler.Handle(
            new GetForecastSpatialDataQuery { ForecastRunId = forecastRunId },
            default)).Single();

        result.Precinct.ShouldBe((int)Barangay.Bayanan);
        result.ClusterId.ShouldBe((uint)12);
        result.Latitude.ShouldBe(14.3856);
        result.Longitude.ShouldBe(121.0189);
        result.Month.ShouldBe(12);
        result.Year.ShouldBe(2026);
        result.PredictedValue.ShouldBe(4.5);
        result.LowerBound.ShouldBe(3);
        result.UpperBound.ShouldBe(6);
        result.Confidence.ShouldBe(0.9);
        result.RiskLevel.ShouldBe("high");
        result.Trend.ShouldBe("increasing");
    }
}
