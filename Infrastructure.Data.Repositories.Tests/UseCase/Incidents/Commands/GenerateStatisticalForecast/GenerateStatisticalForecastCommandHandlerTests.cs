using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Application.UseCase.Incidents.Commands.GenerateStatisticalForecast;
using espasyo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.Incidents.Commands.GenerateStatisticalForecast;

public class GenerateStatisticalForecastCommandHandlerTests
{
    private readonly Mock<IMachineLearningService> _mlServiceMock;
    private readonly Mock<ISpatialForecastService> _spatialServiceMock;
    private readonly Mock<ISeasonalForecastService> _seasonalServiceMock;
    private readonly Mock<ILogger<GenerateStatisticalForecastCommandHandler>> _loggerMock;
    private readonly GenerateStatisticalForecastCommandHandler _handler;

    public GenerateStatisticalForecastCommandHandlerTests()
    {
        _mlServiceMock = new Mock<IMachineLearningService>();
        _spatialServiceMock = new Mock<ISpatialForecastService>();
        _seasonalServiceMock = new Mock<ISeasonalForecastService>();
        _loggerMock = new Mock<ILogger<GenerateStatisticalForecastCommandHandler>>();

        _handler = new GenerateStatisticalForecastCommandHandler(
            _mlServiceMock.Object,
            _spatialServiceMock.Object,
            _seasonalServiceMock.Object,
            _loggerMock.Object
        );
    }

    private static List<ClusterGroup> CreateClusterData()
    {
        return
        [
            new ClusterGroup
            {
                ClusterId = 1,
                ClusterItems =
                [
                    new() { CaseId = "C001", Latitude = 14.5, Longitude = 121.0, Month = 6, Day = 15, Year = 2026, TimeOfDay = "Morning", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Robbery, ClusterId = 1 },
                    new() { CaseId = "C002", Latitude = 14.6, Longitude = 121.1, Month = 6, Day = 16, Year = 2026, TimeOfDay = "Afternoon", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Robbery, ClusterId = 1 },
                    new() { CaseId = "C003", Latitude = 14.7, Longitude = 121.2, Month = 6, Day = 17, Year = 2026, TimeOfDay = "Evening", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Robbery, ClusterId = 1 }
                ]
            }
        ];
    }

    private static List<ClusterGroup> CreateClusterDataWithTimeOfDayDistribution()
    {
        return
        [
            new ClusterGroup
            {
                ClusterId = 1,
                ClusterItems =
                [
                    new() { CaseId = "C001", Latitude = 14.5, Longitude = 121.0, Month = 6, Day = 15, Year = 2026, TimeOfDay = "Morning", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Assault, ClusterId = 1 },
                    new() { CaseId = "C002", Latitude = 14.6, Longitude = 121.1, Month = 6, Day = 16, Year = 2026, TimeOfDay = "Morning", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Assault, ClusterId = 1 },
                    new() { CaseId = "C003", Latitude = 14.7, Longitude = 121.2, Month = 6, Day = 17, Year = 2026, TimeOfDay = "Evening", Precinct = (Barangay)1, CrimeType = CrimeTypeEnum.Assault, ClusterId = 1 }
                ]
            }
        ];
    }

    private static List<ForecastSeries> CreateSeries(int count, int months)
    {
        return Enumerable.Range(1, count).Select(s => new ForecastSeries
        {
            Precinct = s,
            CrimeType = 1,
            ClusterId = (uint)s,
            Forecasts = Enumerable.Range(0, months).Select(m => new ForecastPoint
            {
                Timestamp = new DateTime(2026, 1, 1).AddMonths(m),
                Forecast = 10 + m * 5,
                LowerBound = 5 + m * 3,
                UpperBound = 15 + m * 7,
                Confidence = Math.Max(0.1, 0.95 - m * 0.05),
                Trend = m > months / 2 ? "increasing" : "stable",
                RiskLevel = m > months / 2 ? "high" : "medium"
            }).ToList()
        }).ToList();
    }

    // ── Input validation ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldThrow_WhenClusterDataIsNull()
    {
        var cmd = new GenerateStatisticalForecastCommand { ClusterData = null!, Horizon = 12, ConfidenceLevel = 0.95 };
        await Should.ThrowAsync<ArgumentException>(() => _handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenClusterDataIsEmpty()
    {
        var cmd = new GenerateStatisticalForecastCommand { ClusterData = [], Horizon = 12, ConfidenceLevel = 0.95 };
        await Should.ThrowAsync<ArgumentException>(() => _handler.Handle(cmd, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task Handle_ShouldThrow_WhenHorizonOutOfRange(int horizon)
    {
        var cmd = new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = horizon, ConfidenceLevel = 0.95 };
        await Should.ThrowAsync<ArgumentException>(() => _handler.Handle(cmd, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1)]
    [InlineData(1.5)]
    public async Task Handle_ShouldThrow_WhenConfidenceLevelOutOfRange(double confidenceLevel)
    {
        var cmd = new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = confidenceLevel };
        await Should.ThrowAsync<ArgumentException>(() => _handler.Handle(cmd, default));
    }

    // ── Successful orchestration ─────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallAllServices()
    {
        var cmd = new GenerateStatisticalForecastCommand
        {
            ClusterData = CreateClusterData(),
            Horizon = 12,
            ConfidenceLevel = 0.9,
            ModelType = "ssa",
            IncludeTimeOfDay = true
        };

        var series = CreateSeries(2, 6);
        var spatial = new List<SpatialForecastRow>
        {
            new() { Precinct = 1, Forecast = 5, LowerBound = 3, UpperBound = 7, Confidence = 0.9, Timestamp = DateTime.UtcNow }
        };
        var seasonal = new List<SeasonalPredictionRow>
        {
            new() { Precinct = 1, CrimeType = 1, PeakMonth = 7, TroughMonth = 1 }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(cmd.ClusterData, It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series, Metrics = new ForecastMetrics { ModelAccuracy = 0.85 }, ModelUsed = "ssa" });
        _spatialServiceMock.Setup(x => x.DistributeForecast(cmd.ClusterData, It.IsAny<ForecastParameters>(), series))
            .ReturnsAsync(spatial);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(cmd.ClusterData, It.IsAny<ForecastParameters>()))
            .ReturnsAsync(seasonal);

        var result = await _handler.Handle(cmd, default);

        result.ShouldNotBeNull();
        result.Series.ShouldBe(series);
        result.Spatial.ShouldBe(spatial);
        result.SeasonalPredictions.ShouldBe(seasonal);
        result.ModelUsed.ShouldBe("ssa");
        result.Forecasts.ShouldNotBeEmpty();
        result.Summary.ShouldNotBeNull();
        result.Explanation.ShouldNotBeNull();

        _mlServiceMock.Verify(x => x.GenerateStatisticalForecast(cmd.ClusterData, It.IsAny<ForecastParameters>()), Times.Once);
        _spatialServiceMock.Verify(x => x.DistributeForecast(cmd.ClusterData, It.IsAny<ForecastParameters>(), series), Times.Once);
        _seasonalServiceMock.Verify(x => x.PredictSeasonal(cmd.ClusterData, It.IsAny<ForecastParameters>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectParameters_ToMachineLearningService()
    {
        ForecastParameters captured = null!;
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = CreateSeries(1, 6) })
            .Callback<IEnumerable<ClusterGroup>, ForecastParameters>((_, p) => captured = p);
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var cmd = new GenerateStatisticalForecastCommand
        {
            ClusterData = CreateClusterData(),
            Horizon = 18,
            ConfidenceLevel = 0.8,
            ModelType = "seasonal",
            IncludeSeasonality = false,
            WeightRecentData = false,
            IncludeTrend = true,
            IncludeTimeOfDay = true,
            IncludeMonthOfYear = true
        };

        await _handler.Handle(cmd, default);

        captured.ShouldNotBeNull();
        captured.Horizon.ShouldBe(18);
        captured.ConfidenceLevel.ShouldBe(0.8);
        captured.ModelType.ShouldBe("seasonal");
        captured.IncludeSeasonality.ShouldBeFalse();
        captured.WeightRecentData.ShouldBeFalse();
        captured.IncludeTrend.ShouldBeTrue();
        captured.IncludeTimeOfDay.ShouldBeTrue();
        captured.IncludeMonthOfYear.ShouldBeTrue();
    }

    // ── Error propagation ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldPropagateException_FromMachineLearningService()
    {
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ThrowsAsync(new InvalidOperationException("ML service failure"));

        var cmd = new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 };
        await Should.ThrowAsync<InvalidOperationException>(() => _handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_FromSpatialService()
    {
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = CreateSeries(1, 6) });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ThrowsAsync(new InvalidOperationException("Spatial failure"));

        var cmd = new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 };
        await Should.ThrowAsync<InvalidOperationException>(() => _handler.Handle(cmd, default));
    }

    // ── FlattenTemporalSeries (tested indirectly) ────────────────────

    [Fact]
    public async Task Handle_ShouldFlattenWithoutTimeOfDay()
    {
        var series = new List<ForecastSeries>
        {
            new()
            {
                Precinct = 1, CrimeType = 2, ClusterId = 1,
                Forecasts =
                [
                    new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 5, LowerBound = 3, UpperBound = 7, Confidence = 0.9, Trend = "stable", RiskLevel = "medium" },
                    new() { Timestamp = new DateTime(2026, 9, 1), Forecast = 6, LowerBound = 4, UpperBound = 8, Confidence = 0.85, Trend = "increasing", RiskLevel = "high" }
                ]
            }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95, IncludeTimeOfDay = false }, default);

        var rows = result.Forecasts;
        rows.Count.ShouldBe(2);
        rows.ShouldAllBe(r => r.PredictionType == "temporal");
        rows.ShouldAllBe(r => r.TimeOfDay == null);
        rows[0].Forecast.ShouldBe(5);
        rows[0].Precinct.ShouldBe(1);
        rows[0].CrimeType.ShouldBe(2);
        rows[0].Trend.ShouldBe("stable");
        rows[0].RiskLevel.ShouldBe("medium");
        rows[1].Forecast.ShouldBe(6);
        rows[1].Trend.ShouldBe("increasing");
        rows[1].RiskLevel.ShouldBe("high");
    }

    [Fact]
    public async Task Handle_ShouldFlattenWithTimeOfDay()
    {
        var series = new List<ForecastSeries>
        {
            new()
            {
                Precinct = 1, CrimeType = 1, ClusterId = 1,
                Forecasts =
                [
                    new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 9, LowerBound = 6, UpperBound = 12, Confidence = 0.9, Trend = "stable", RiskLevel = "low" }
                ]
            }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterDataWithTimeOfDayDistribution(), Horizon = 12, ConfidenceLevel = 0.95, IncludeTimeOfDay = true }, default);

        result.Forecasts.Count.ShouldBe(3);
        result.Forecasts[0].TimeOfDay.ShouldBe("Morning");
        result.Forecasts[1].TimeOfDay.ShouldBe("Afternoon");
        result.Forecasts[2].TimeOfDay.ShouldBe("Evening");
        result.Forecasts[0].Forecast.ShouldBeGreaterThan(0);
        result.Forecasts[0].Forecast.ShouldBeLessThan(9);
        result.Forecasts[2].Forecast.ShouldBeGreaterThan(0);
        result.Forecasts[2].Forecast.ShouldBeLessThan(9);
    }

    // ── CompositeRiskScore ───────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldPopulateCompositeRiskScore_OnEachRow()
    {
        // Precinct 1 (Bayanan, risk 0.7), CrimeType 2 (Burglary, severity 3.0)
        // No heinous in scope → multiplier 1.0
        // score = (3.0 / 10.0) * 0.7 * 1.0 = 0.21
        var series = new List<ForecastSeries>
        {
            new() { Precinct = 1, CrimeType = 2, ClusterId = 1,
                Forecasts = [new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 5 }] }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 }, default);

        result.Forecasts[0].CompositeRiskScore.ShouldBe(0.21, 0.001);
        result.Summary.AverageCompositeRiskScore.ShouldBe(0.21, 0.001);
        result.Summary.MaxCompositeRiskScore.ShouldBe(0.21, 0.001);
    }

    [Fact]
    public async Task Handle_ShouldBoostHeinousCrimeScore_WhenHeinousPresent()
    {
        // Precinct 0 (Alabang, risk 1.8), CrimeType 15 (Murder, severity 10.0, heinous)
        // Heinous in scope via CrimeTypeFilter → boost = 1.5
        // score = (10.0 / 10.0) * 1.8 * 1.5 = 2.7
        var series = new List<ForecastSeries>
        {
            new() { Precinct = 0, CrimeType = 15, ClusterId = 1,
                Forecasts = [new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 10 }] }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand
        {
            ClusterData = CreateClusterData(),
            Horizon = 12,
            ConfidenceLevel = 0.95,
            CrimeTypeFilter = ["15"] // Murder is heinous and in the filter → boost applies
        }, default);

        result.Forecasts[0].CompositeRiskScore.ShouldBe(2.7, 0.001);
    }

    [Fact]
    public async Task Should_ApplyHeinousPresenceFactor_ToNonHeinousCrime_WhenHeinousInScope()
    {
        // Precinct 0 (Alabang, risk 1.8), CrimeType 1 (Assault, severity 3.0, NOT heinous)
        // Heinous in scope via CrimeTypeFilter → presence factor 1.2
        // score = (3.0 / 10.0) * 1.8 * 1.2 = 0.648
        var series = new List<ForecastSeries>
        {
            new() { Precinct = 0, CrimeType = 1, ClusterId = 1,
                Forecasts = [new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 5 }] }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand
        {
            ClusterData = CreateClusterData(),
            Horizon = 12,
            ConfidenceLevel = 0.95,
            CrimeTypeFilter = ["1", "15"] // non-heinous (1) with heinous (15) → presence factor applies to non-heinous
        }, default);

        result.Forecasts[0].CompositeRiskScore.ShouldBe(0.648, 0.001);
    }

    [Fact]
    public async Task Should_RespectConfigOverrides()
    {
        // CrimeType 15 (Murder, default severity 10.0), Precinct 0 (Alabang, default risk 1.8)
        // Override: HeinousBoostFactor = 2.0, custom severity = 5.0 for crime 15
        // Heinous in scope (via CrimeTypeFilter)
        // score = (5.0 / 10.0) * 1.8 * 2.0 = 1.8
        var series = new List<ForecastSeries>
        {
            new() { Precinct = 0, CrimeType = 15, ClusterId = 1,
                Forecasts = [new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 10 }] }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand
        {
            ClusterData = CreateClusterData(),
            Horizon = 12,
            ConfidenceLevel = 0.95,
            CrimeTypeFilter = ["15"],
            RiskScoringConfig = new RiskScoringConfig
            {
                HeinousBoostFactor = 2.0,
                CrimeTypeSeverityScores = new Dictionary<int, double> { [15] = 5.0 }
            }
        }, default);

        result.Forecasts[0].CompositeRiskScore.ShouldBe(1.8, 0.001);
    }

    [Fact]
    public async Task Should_DefaultScoreToZero_WhenCrimeTypeIsNull()
    {
        var series = new List<ForecastSeries>
        {
            new() { Precinct = 1, CrimeType = 1, ClusterId = 1,
                Forecasts = [new() { Timestamp = new DateTime(2026, 8, 1), Forecast = 5 }] }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        // Row without CrimeType should get score 0
        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 }, default);

        result.Forecasts.All(f => f.CrimeType.HasValue).ShouldBeTrue(); // all have CrimeType in this test
    }

    // ── Forecast summary ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldGenerateSummary_WhenForecastsEmpty()
    {
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = CreateSeries(1, 0) });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 }, default);

        result.Summary.TotalForecasts.ShouldBe(0);
        result.Summary.HighRiskPredictions.ShouldBe(0);
        result.Summary.CriticalRiskPredictions.ShouldBe(0);
        result.Summary.OverallTrend.ShouldBe("stable");
        result.Summary.DominantRiskLevel.ShouldBe("low");
        result.Summary.AverageConfidence.ShouldBe(0);
        result.Summary.KeyInsight.ShouldContain("Not enough historical data");
        result.Summary.RecommendedActions.ShouldContain(a => a.Contains("Collect more incident data"));
    }

    [Fact]
    public async Task Handle_ShouldGenerateSummary_WithMixedTrends()
    {
        var series = new List<ForecastSeries>
        {
            new()
            {
                Precinct = 1, CrimeType = 1, ClusterId = 1,
                Forecasts = Enumerable.Range(0, 12).Select(i => new ForecastPoint
                {
                    Timestamp = new DateTime(2026, i + 1, 1),
                    Forecast = i * 2,
                    LowerBound = i,
                    UpperBound = i * 3,
                    Confidence = 0.9,
                    Trend = i < 5 ? "decreasing" : "increasing",
                    RiskLevel = i >= 9 ? "critical" : i >= 6 ? "high" : i >= 3 ? "medium" : "low"
                }).ToList()
            }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 }, default);

        result.Summary.TotalForecasts.ShouldBe(12);
        result.Summary.HighRiskPredictions.ShouldBeGreaterThan(0);
        result.Summary.CriticalRiskPredictions.ShouldBe(3);
        result.Summary.OverallTrend.ShouldBe("increasing");
        result.Summary.DominantRiskLevel.ShouldBe("critical");
        result.Summary.AverageConfidence.ShouldBe(0.9, 0.01);
        result.Summary.KeyInsight.ShouldContain("Alert");
        result.Summary.RecommendedActions.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldGenerateSummary_WhenDecreasingTrend()
    {
        var series = new List<ForecastSeries>
        {
            new()
            {
                Precinct = 1, CrimeType = 1, ClusterId = 1,
                Forecasts = Enumerable.Range(0, 6).Select(i => new ForecastPoint
                {
                    Timestamp = new DateTime(2026, i + 1, 1),
                    Forecast = 10,
                    LowerBound = 5,
                    UpperBound = 15,
                    Confidence = 0.9,
                    Trend = "decreasing",
                    RiskLevel = "low"
                }).ToList()
            }
        };

        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = series });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95 }, default);

        result.Summary.OverallTrend.ShouldBe("decreasing");
        result.Summary.KeyInsight.ShouldContain("Positive");
        result.Summary.KeyInsight.ShouldContain("declining");
    }

    // ── Forecast explanation ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldGenerateExplanation_ByModelType()
    {
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = CreateSeries(1, 6), Metrics = new ForecastMetrics { ModelAccuracy = 0.82 } });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var cmd = new GenerateStatisticalForecastCommand { ClusterData = CreateClusterData(), Horizon = 12, ConfidenceLevel = 0.95, ModelType = "ensemble" };
        var result = await _handler.Handle(cmd, default);

        result.Explanation.ModelDescription.ShouldContain("Statistical forecasting model");
        result.Explanation.DataQualityNotes.ShouldContain("82");
        result.Explanation.ConfidenceExplanation.ShouldNotBeNullOrEmpty();
        result.Explanation.TrendAnalysis.ShouldNotBeNullOrEmpty();
        result.Explanation.RiskAssessmentLogic.ShouldNotBeNullOrEmpty();
        result.Explanation.LimitationsAndCaveats.ShouldNotBeNullOrEmpty();
        result.Explanation.HowToInterpret.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIncludeDataQualityNote_WhenFewDataPoints()
    {
        var clusterData = CreateClusterData();
        _mlServiceMock.Setup(x => x.GenerateStatisticalForecast(clusterData, It.IsAny<ForecastParameters>()))
            .ReturnsAsync(new ForecastResponse { Series = CreateSeries(1, 6), Metrics = new ForecastMetrics { ModelAccuracy = 0.7 } });
        _spatialServiceMock.Setup(x => x.DistributeForecast(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>(), It.IsAny<List<ForecastSeries>>()))
            .ReturnsAsync([]);
        _seasonalServiceMock.Setup(x => x.PredictSeasonal(It.IsAny<IEnumerable<ClusterGroup>>(), It.IsAny<ForecastParameters>()))
            .ReturnsAsync([]);

        var cmd = new GenerateStatisticalForecastCommand { ClusterData = clusterData, Horizon = 12, ConfidenceLevel = 0.95 };
        var result = await _handler.Handle(cmd, default);

        // 3 cluster items → dataPoints = 3, < 24 → limitations mention "Limited historical data"
        result.Explanation.LimitationsAndCaveats.ShouldContain("Limited historical data");
    }
}
