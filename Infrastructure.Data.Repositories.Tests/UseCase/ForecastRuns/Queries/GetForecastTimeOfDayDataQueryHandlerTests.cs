using System.Text.Json;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastTimeOfDayData;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.ForecastRuns.Queries;

public class GetForecastTimeOfDayDataQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSavedTimeOfDayForecastAllocations()
    {
        var forecastRunId = Guid.NewGuid();
        var timeOfDayForecasts = new List<TimeOfDayPredictionDto>
        {
            new()
            {
                Precinct = (int)Barangay.Bayanan,
                CrimeType = (int)CrimeTypeEnum.Robbery,
                Month = 12,
                Year = 2026,
                TimeOfDay = "Morning",
                Forecast = 2.5,
                Trend = "increasing"
            }
        };
        var runData = new ForecastRunSnapshotData
        {
            HistoricalData = new List<HistoricalDataPointDto>(),
            TimeOfDayPredictions = timeOfDayForecasts
        };
        var run = new ForecastRun(
            forecastRunId,
            6,
            0.95,
            ForecastModelTypeEnum.Linear,
            "user",
            historicalDataJson: JsonSerializer.Serialize(runData));
        var repository = new Mock<IForecastRepository>();
        repository.Setup(repo => repo.GetForecastRunByIdAsync(forecastRunId))
            .ReturnsAsync(run);

        var handler = new GetForecastTimeOfDayDataQueryHandler(repository.Object);

        var result = (await handler.Handle(
            new GetForecastTimeOfDayDataQuery { ForecastRunId = forecastRunId },
            default)).Single();

        result.Precinct.ShouldBe((int)Barangay.Bayanan);
        result.CrimeType.ShouldBe((int)CrimeTypeEnum.Robbery);
        result.Month.ShouldBe(12);
        result.Year.ShouldBe(2026);
        result.TimeOfDay.ShouldBe("Morning");
        result.Forecast.ShouldBe(2.5);
        result.Trend.ShouldBe("increasing");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyForLegacySnapshotWithoutTimeOfDayForecasts()
    {
        var forecastRunId = Guid.NewGuid();
        var run = new ForecastRun(
            forecastRunId,
            6,
            0.95,
            ForecastModelTypeEnum.Linear,
            "user",
            historicalDataJson: JsonSerializer.Serialize(new List<HistoricalDataPointDto>()));
        var repository = new Mock<IForecastRepository>();
        repository.Setup(repo => repo.GetForecastRunByIdAsync(forecastRunId))
            .ReturnsAsync(run);

        var handler = new GetForecastTimeOfDayDataQueryHandler(repository.Object);

        var result = await handler.Handle(
            new GetForecastTimeOfDayDataQuery { ForecastRunId = forecastRunId },
            default);

        result.ShouldBeEmpty();
    }
}
