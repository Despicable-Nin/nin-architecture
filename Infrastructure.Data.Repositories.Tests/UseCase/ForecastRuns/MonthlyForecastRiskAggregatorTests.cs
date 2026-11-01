using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastRiskPeriods;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.ForecastRuns;

public class MonthlyForecastRiskAggregatorTests
{
    [Fact]
    public void Aggregate_GroupsByCalendarMonthAndBackendRiskLevel()
    {
        var periods = MonthlyForecastRiskAggregator.Aggregate(
        [
            new(2026, 12, 2.5, "low"),
            new(2026, 12, 1.5, "medium"),
            new(2026, 12, 3.0, "medium"),
            new(2027, 1, 4.0, "critical")
        ]);

        periods.Count.ShouldBe(2);
        periods[0].Year.ShouldBe(2026);
        periods[0].Month.ShouldBe(12);
        periods[0].ProjectedIncidents.ShouldBe(7.0);
        periods[0].ForecastByRiskLevel.Low.ShouldBe(2.5);
        periods[0].ForecastByRiskLevel.Medium.ShouldBe(4.5);
        periods[0].ForecastByRiskLevel.High.ShouldBe(0);
        periods[0].ForecastByRiskLevel.Critical.ShouldBe(0);
        periods[0].ProjectedIncidents.ShouldBe(
            periods[0].ForecastByRiskLevel.Low +
            periods[0].ForecastByRiskLevel.Medium +
            periods[0].ForecastByRiskLevel.High +
            periods[0].ForecastByRiskLevel.Critical);
        periods[1].Year.ShouldBe(2027);
        periods[1].Month.ShouldBe(1);
        periods[1].ForecastByRiskLevel.Critical.ShouldBe(4.0);
    }

    [Fact]
    public void Aggregate_ReturnsNoPeriods_WhenNoForecastRowsExist()
    {
        MonthlyForecastRiskAggregator.Aggregate([]).ShouldBeEmpty();
    }

    [Fact]
    public void Aggregate_RejectsUnknownBackendRiskLevel()
    {
        Should.Throw<ArgumentException>(() =>
            MonthlyForecastRiskAggregator.Aggregate([new(2026, 12, 3, "urgent")]));
    }

    [Fact]
    public async Task GetForecastRiskPeriodsQuery_UsesPersistedForecastResults()
    {
        var runId = Guid.NewGuid();
        var repository = new Mock<IForecastRepository>();
        repository
            .Setup(repo => repo.GetForecastResultsAsync(runId))
            .ReturnsAsync(
            [
                new ForecastResult(runId, (Barangay)1, CrimeTypeEnum.Assault, 8, 2026, 2.25, 1, 3, 0.9, "high", "stable"),
                new ForecastResult(runId, (Barangay)2, CrimeTypeEnum.Robbery, 8, 2026, 1.75, 1, 3, 0.9, "high", "stable"),
                new ForecastResult(runId, (Barangay)1, CrimeTypeEnum.Assault, 9, 2026, 5, 4, 6, 0.9, "medium", "stable")
            ]);

        var handler = new GetForecastRiskPeriodsQueryHandler(repository.Object);
        var periods = await handler.Handle(new GetForecastRiskPeriodsQuery { ForecastRunId = runId }, default);

        periods.Count.ShouldBe(2);
        periods[0].ProjectedIncidents.ShouldBe(4);
        periods[0].ForecastByRiskLevel.High.ShouldBe(4);
        repository.Verify(repo => repo.GetForecastResultsAsync(runId), Times.Once);
    }
}
