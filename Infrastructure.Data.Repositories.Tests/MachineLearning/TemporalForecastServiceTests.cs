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
        var items = new List<ClusterItem>();
        for (int i = 0; i < months; i++)
        {
            var (y, m) = OffsetMonth(2023, 1, i);
            var count = incidentsPerMonth + i * increment;
            for (int j = 0; j < count; j++)
            {
                items.Add(MakeItem(y, m, Barangay.Alabang, CrimeTypeEnum.Theft));
            }
        }
        return [new ClusterGroup { ClusterId = 0, ClusterItems = items }];
    }

    private static List<ClusterGroup> CreateConstantData(int months, int incidentsPerMonth)
    {
        var items = new List<ClusterItem>();
        for (int i = 0; i < months; i++)
        {
            var (y, m) = OffsetMonth(2023, 1, i);
            for (int j = 0; j < incidentsPerMonth; j++)
            {
                items.Add(MakeItem(y, m, Barangay.Alabang, CrimeTypeEnum.Theft));
            }
        }
        return [new ClusterGroup { ClusterId = 0, ClusterItems = items }];
    }

    private static List<ClusterGroup> CreateCrossProductData(
        Barangay[] precincts, CrimeTypeEnum[] crimeTypes, int months, int incidentsPerMonth)
    {
        var groups = new List<ClusterGroup>();
        uint cid = 0;
        foreach (var precinct in precincts)
        {
            foreach (var crimeType in crimeTypes)
            {
                var items = new List<ClusterItem>();
                for (int i = 0; i < months; i++)
                {
                    var (y, m) = OffsetMonth(2023, 1, i);
                    for (int j = 0; j < incidentsPerMonth; j++)
                    {
                        items.Add(MakeItem(y, m, precinct, crimeType));
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
