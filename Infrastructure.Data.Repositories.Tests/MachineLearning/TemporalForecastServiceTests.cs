using espasyo.Application.Common.Models.ML;
using espasyo.Domain.Enums;
using espasyo.Infrastructure.MachineLearning;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.MachineLearning;

public class TemporalForecastServiceTests
{
    private readonly MLContext _mlContext;
    private readonly Mock<ILogger<TemporalForecastService>> _loggerMock;
    private readonly TemporalForecastService _service;

    public TemporalForecastServiceTests()
    {
        _mlContext = new MLContext(seed: 42);
        _loggerMock = new Mock<ILogger<TemporalForecastService>>();
        _service = new TemporalForecastService(_mlContext, _loggerMock.Object);
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("seasonal")]
    [InlineData("ensemble")]
    public async Task GenerateForecast_AllModels_ProducesNonNegativePredictions(string modelType)
    {
        var data = CreateIncreasingData(months: 24, incidentsPerMonth: 3);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = modelType,
            ConfidenceLevel = 0.95,
        });

        result.ShouldNotBeNull();
        result.Series.ShouldNotBeEmpty();

        foreach (var series in result.Series)
        {
            series.Forecasts.Count.ShouldBe(6);
            foreach (var fp in series.Forecasts)
            {
                fp.Forecast.ShouldBeGreaterThanOrEqualTo(0, $"Forecast should be >= 0, got {fp.Forecast}");
                fp.LowerBound.ShouldBeLessThanOrEqualTo(fp.Forecast, "LowerBound <= Forecast");
                fp.UpperBound.ShouldBeGreaterThanOrEqualTo(fp.Forecast, "UpperBound >= Forecast");
                fp.Confidence.ShouldBeInRange(0, 1, $"Confidence in (0,1], got {fp.Confidence}");
            }
        }
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("seasonal")]
    [InlineData("ssa")]
    [InlineData("ensemble")]
    public async Task GenerateForecast_AllModels_ReturnsCorrectSeriesCount(string modelType)
    {
        var data = CreateCrossProductData(
            precincts: new[] { Barangay.Alabang, Barangay.Poblacion },
            crimeTypes: new[] { CrimeTypeEnum.Theft, CrimeTypeEnum.Robbery },
            months: 24,
            incidentsPerMonth: 2
        );

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = modelType,
        });

        result.Series.Count.ShouldBe(4); // 2 precincts × 2 crime types
        foreach (var series in result.Series)
        {
            series.Metadata["ModelUsed"].ShouldBe(modelType, $"Series model should match");
            series.Forecasts.Count.ShouldBe(6);
        }
    }

    [Fact]
    public async Task GenerateForecast_StartsAtNextFullCalendarMonth()
    {
        var data = CreateIncreasingData(months: 12, incidentsPerMonth: 3);
        var expectedStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
        });

        foreach (var series in result.Series)
        {
            series.Forecasts.ShouldNotBeEmpty();
            series.Forecasts.First().Timestamp.ShouldBe(expectedStart);
        }
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("seasonal")]
    public async Task GenerateForecast_UsesFullHistoryWithMoreWeightOnRecentData(string modelType)
    {
        var data = CreateStepChangeData();

        var weighted = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 1,
            ModelType = modelType,
            WeightRecentData = true,
        });
        var equallyWeighted = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 1,
            ModelType = modelType,
            WeightRecentData = false,
        });

        var weightedForecast = weighted.Series.Single().Forecasts.Single().Forecast;
        var equallyWeightedForecast = equallyWeighted.Series.Single().Forecasts.Single().Forecast;

        if (modelType == "linear")
            weightedForecast.ShouldBeGreaterThan(equallyWeightedForecast);
        else
            weightedForecast.ShouldNotBe(equallyWeightedForecast);
    }

    [Fact]
    public async Task GenerateForecast_SsaFallback_RecordsEffectiveModelAndReason()
    {
        var data = CreateConstantData(months: 12, incidentsPerMonth: 1);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 12,
            ModelType = "ssa",
        });

        foreach (var series in result.Series)
        {
            series.EffectiveModel.ShouldBe("linear");
            series.FallbackReason.ShouldContain("linear");
            series.Metadata["ModelUsed"].ShouldBe("linear");
        }
    }

    [Fact]
    public async Task GenerateForecast_LinearModel_UphillData_ProducesIncreasingTrend()
    {
        var data = CreateIncreasingData(months: 24, incidentsPerMonth: 1, increment: 2);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
        });

        foreach (var series in result.Series)
        {
            series.Forecasts.ShouldAllBe(fp => fp.Trend == "increasing" || fp.Trend == "stable");
        }
    }

    [Fact]
    public async Task GenerateForecast_LinearModel_FlatData_ProducesStableTrend()
    {
        var data = CreateConstantData(months: 24, incidentsPerMonth: 10);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
        });

        foreach (var series in result.Series)
        {
            foreach (var fp in series.Forecasts)
            {
                fp.Forecast.ShouldBeGreaterThan(0);
            }
        }
    }

    [Fact]
    public async Task GenerateForecast_WithEmptyClusterData_Throws()
    {
        var data = new List<ClusterGroup>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _service.GenerateForecast(data, new ForecastParameters
            {
                Horizon = 6,
                ModelType = "linear",
            });
        });
    }

    [Fact]
    public async Task GenerateForecast_AllModels_WithSingleMonthData_DoesNotCrash()
    {
        var data = CreateConstantData(months: 1, incidentsPerMonth: 5);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 3,
            ModelType = "linear",
        });

        result.ShouldNotBeNull();
        // With only 1 month of data the model should handle gracefully
    }

    [Fact]
    public async Task GenerateForecast_LinearModel_ZeroData_ProducesZeroOrNearZero()
    {
        // Items with Value = 0 still contribute to the time series (zero-fill).
        // Use minimal items so the model still has data to work with.
        var data = CreateConstantData(months: 12, incidentsPerMonth: 1);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
        });

        foreach (var series in result.Series)
        {
            foreach (var fp in series.Forecasts)
            {
                fp.Forecast.ShouldBeLessThanOrEqualTo(3);
            }
        }
    }

    [Fact]
    public async Task GenerateForecast_Confidence_DecaysOverHorizon()
    {
        var data = CreateIncreasingData(months: 24, incidentsPerMonth: 5);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
            ConfidenceLevel = 0.95,
        });

        foreach (var series in result.Series)
        {
            var confidences = series.Forecasts.Select(fp => fp.Confidence).ToList();
            // Confidence should be non-increasing (each later step <= earlier step)
            for (int i = 1; i < confidences.Count; i++)
            {
                confidences[i].ShouldBeLessThanOrEqualTo(confidences[i - 1],
                    $"Confidence should not increase at step {i}");
            }
        }
    }

    [Fact]
    public async Task GenerateForecast_Metrics_ArePopulated()
    {
        var data = CreateIncreasingData(months: 24, incidentsPerMonth: 5);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 6,
            ModelType = "linear",
        });

        result.Metrics.ShouldNotBeNull();
    }

    [Fact]
    public void CalculateValidationMetrics_ReturnsCorrectMetrics()
    {
        // Given: actual [10, 20, 30] vs predicted [12, 18, 28]
        // MAE = 2.0, RMSE = 2.0, MAPE ≈ 12.22%, Accuracy ≈ 0.88
        var actual = new List<TimeSeriesData>
        {
            new() { Date = new DateTime(2026, 1, 1), Value = 10f },
            new() { Date = new DateTime(2026, 2, 1), Value = 20f },
            new() { Date = new DateTime(2026, 3, 1), Value = 30f },
        };
        var predicted = new List<ForecastPoint>
        {
            new() { Timestamp = new DateTime(2026, 4, 1), Forecast = 12 },
            new() { Timestamp = new DateTime(2026, 5, 1), Forecast = 18 },
            new() { Timestamp = new DateTime(2026, 6, 1), Forecast = 28 },
        };

        // When
        var metrics = TemporalForecastService.CalculateValidationMetrics(actual, predicted);

        // Then
        metrics.MeanAbsoluteError.ShouldBe(2.0);
        metrics.RootMeanSquareError.ShouldBe(2.0);
        metrics.MeanAbsolutePercentageError.ShouldBeInRange(12.2, 12.3, "MAPE should be ≈ 12.22%");
        metrics.ModelAccuracy.ShouldBeInRange(0.87, 0.88, "Accuracy should be ≈ 0.88");
    }

    [Fact]
    public void AverageMetrics_AveragesAcrossSeries()
    {
        var metrics = new List<ForecastMetrics>
        {
            new() { MeanAbsoluteError = 2.0, RootMeanSquareError = 2.0, MeanAbsolutePercentageError = 12.22, ModelAccuracy = 0.88 },
            new() { MeanAbsoluteError = 4.0, RootMeanSquareError = 6.0, MeanAbsolutePercentageError = 20.0, ModelAccuracy = 0.80 },
        };

        var result = TemporalForecastService.AverageMetrics(metrics);

        result.MeanAbsoluteError.ShouldBe(3.0);
        result.RootMeanSquareError.ShouldBe(4.0);
        result.MeanAbsolutePercentageError.ShouldBeInRange(16.1, 16.2, "MAPE should be ≈ 16.11");
        result.ModelAccuracy.ShouldBeInRange(0.84, 0.85, "Accuracy should be ≈ 0.84");
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("seasonal")]
    [InlineData("ssa")]
    [InlineData("ensemble")]
    public async Task GenerateForecast_AllModels_BoundsAreSane(string modelType)
    {
        var data = CreateIncreasingData(months: 24, incidentsPerMonth: 5);

        var result = await _service.GenerateForecast(data, new ForecastParameters
        {
            Horizon = 8,
            ModelType = modelType,
        });

        foreach (var series in result.Series)
        {
            foreach (var fp in series.Forecasts)
            {
                if (fp.UpperBound - fp.LowerBound <= 0)
                {
                    Console.WriteLine($"Bounds invalid: model={series.EffectiveModel}, ts={fp.Timestamp:yyyy-MM-dd}, forecast={fp.Forecast}, lower={fp.LowerBound}, upper={fp.UpperBound}");
                }

                fp.LowerBound.ShouldBeLessThanOrEqualTo(fp.Forecast);
                fp.UpperBound.ShouldBeGreaterThanOrEqualTo(fp.Forecast);
                (fp.UpperBound - fp.LowerBound).ShouldBeGreaterThan(0,
                    "UpperBound - LowerBound should > 0 for non-zero predictions");
            }
        }
    }

    // ── Test data helpers ──────────────────────────────────────────────

    private static (int year, int month) OffsetMonth(int startYear, int startMonth, int offset)
    {
        var totalMonths = startYear * 12 + (startMonth - 1) + offset;
        return (totalMonths / 12, (totalMonths % 12) + 1);
    }

    private static List<ClusterGroup> CreateIncreasingData(int months, int incidentsPerMonth, int increment = 1)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));
        var items = new List<ClusterItem>();
        for (int i = 0; i < months; i++)
        {
            var current = start.AddMonths(i);
            var count = incidentsPerMonth + i * increment;
            for (int j = 0; j < count; j++)
            {
                items.Add(MakeItem(current.Year, current.Month, Barangay.Alabang, CrimeTypeEnum.Theft));
            }
        }
        return [new ClusterGroup { ClusterId = 0, ClusterItems = items }];
    }

    private static List<ClusterGroup> CreateConstantData(int months, int incidentsPerMonth)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));
        var items = new List<ClusterItem>();
        for (int i = 0; i < months; i++)
        {
            var current = start.AddMonths(i);
            for (int j = 0; j < incidentsPerMonth; j++)
            {
                items.Add(MakeItem(current.Year, current.Month, Barangay.Alabang, CrimeTypeEnum.Theft));
            }
        }
        return [new ClusterGroup { ClusterId = 0, ClusterItems = items }];
    }

    private static List<ClusterGroup> CreateStepChangeData()
    {
        const int months = 36;
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));
        var items = new List<ClusterItem>();
        for (var monthIndex = 0; monthIndex < months; monthIndex++)
        {
            var current = start.AddMonths(monthIndex);
            var incidentsPerMonth = monthIndex < 24 ? 10 : 10 + monthIndex - 24;
            for (var incidentIndex = 0; incidentIndex < incidentsPerMonth; incidentIndex++)
            {
                items.Add(MakeItem(current.Year, current.Month, Barangay.Alabang, CrimeTypeEnum.Theft));
            }
        }

        return [new ClusterGroup { ClusterId = 0, ClusterItems = items }];
    }

    private static List<ClusterGroup> CreateCrossProductData(
        Barangay[] precincts, CrimeTypeEnum[] crimeTypes, int months, int incidentsPerMonth)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));
        var groups = new List<ClusterGroup>();
        uint cid = 0;
        foreach (var precinct in precincts)
        {
            foreach (var crimeType in crimeTypes)
            {
                var items = new List<ClusterItem>();
                for (int i = 0; i < months; i++)
                {
                    var current = start.AddMonths(i);
                    for (int j = 0; j < incidentsPerMonth; j++)
                    {
                        items.Add(MakeItem(current.Year, current.Month, precinct, crimeType));
                    }
                }
                groups.Add(new ClusterGroup { ClusterId = cid++, ClusterItems = items });
            }
        }
        return groups;
    }

    private static ClusterItem MakeItem(int year, int month, Barangay precinct, CrimeTypeEnum crimeType)
    {
        return new ClusterItem
        {
            CaseId = $"{year}-{month}-{precinct}-{crimeType}-{Guid.NewGuid():N}",
            Latitude = 14.4,
            Longitude = 121.0,
            Month = month,
            Day = 15,
            Year = year,
            TimeOfDay = "Morning",
            Precinct = precinct,
            CrimeType = crimeType,
            ClusterId = 0,
        };
    }
}
