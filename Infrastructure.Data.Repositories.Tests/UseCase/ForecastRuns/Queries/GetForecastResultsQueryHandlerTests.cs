using System.Text.Json;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastResults;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.ForecastRuns.Queries;

public class GetForecastResultsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPriorYearActualFromSavedHistoricalData()
    {
        var forecastRunId = Guid.NewGuid();
        var historicalData = new List<HistoricalDataPointDto>
        {
            new()
            {
                Precinct = (int)Barangay.Bayanan,
                CrimeType = (int)CrimeTypeEnum.Robbery,
                Year = 2025,
                Month = 12,
                Count = 7
            }
        };
        var run = new ForecastRun(
            Guid.NewGuid(),
            12,
            0.95,
            ForecastModelTypeEnum.Linear,
            "user",
            historicalDataJson: JsonSerializer.Serialize(historicalData));
        var results = new[]
        {
            new ForecastResult(
                forecastRunId,
                Barangay.Bayanan,
                CrimeTypeEnum.Robbery,
                12,
                2026,
                10,
                8,
                12,
                0.9,
                "medium",
                "stable")
        };
        var repository = new Mock<IForecastRepository>();
        repository.Setup(repo => repo.GetForecastResultsAsync(forecastRunId))
            .ReturnsAsync(results);
        repository.Setup(repo => repo.GetForecastRunByIdAsync(forecastRunId))
            .ReturnsAsync(run);

        var handler = new GetForecastResultsQueryHandler(repository.Object);

        var result = (await handler.Handle(new GetForecastResultsQuery { ForecastRunId = forecastRunId }, default))
            .Single();

        result.LastYearActual.ShouldBe(7);
    }
}
