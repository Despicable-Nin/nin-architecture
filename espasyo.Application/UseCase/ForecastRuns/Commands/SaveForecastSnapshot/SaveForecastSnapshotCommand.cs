using espasyo.Application.Common.Models.ML;
using MediatR;
using System.Text.Json;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;

public class SaveForecastSnapshotCommand : IRequest<SaveForecastSnapshotResponse>
{
    public string Name { get; set; } = string.Empty;
    public int ForecastPeriod { get; set; }
    public double ConfidenceLevel { get; set; } = 0.95;
    public string ModelType { get; set; } = "SSA";
    public string? RequestedModelType { get; set; }
    public string? EffectiveModelType { get; set; }
    public string? ModelFallbackReason { get; set; }
    public List<HistoricalDataPointDto> HistoricalData { get; set; } = new();
    public List<TimeOfDayPredictionDto> TimeOfDayPredictions { get; set; } = new();
    public List<PredictionDto> Predictions { get; set; } = new();
    public List<SpatialPredictionDto> SpatialPredictions { get; set; } = new();
    public List<SeasonalPredictionDto> SeasonalPredictions { get; set; } = new();
    public string GeneratedById { get; set; } = string.Empty;
}

public class HistoricalDataPointDto
{
    public int Precinct { get; set; }
    public int CrimeType { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int Count { get; set; }
    public string TimeOfDay { get; set; } = string.Empty;
    public uint? ClusterId { get; set; }
}

public class TimeOfDayPredictionDto
{
    public int Precinct { get; set; }
    public int CrimeType { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string TimeOfDay { get; set; } = string.Empty;
    public double Forecast { get; set; }
    public string Trend { get; set; } = "stable";
}

public class ForecastRunSnapshotData
{
    public List<HistoricalDataPointDto> HistoricalData { get; set; } = new();
    public List<TimeOfDayPredictionDto> TimeOfDayPredictions { get; set; } = new();

    public static ForecastRunSnapshotData Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return new ForecastRunSnapshotData
            {
                HistoricalData = JsonSerializer.Deserialize<List<HistoricalDataPointDto>>(json) ?? new()
            };
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<ForecastRunSnapshotData>(json)
                ?? new ForecastRunSnapshotData();
        }

        if (document.RootElement.ValueKind == JsonValueKind.Null)
            return new ForecastRunSnapshotData();

        throw new JsonException("Forecast snapshot data must be a JSON object or legacy historical-data array.");
    }
}

public class PredictionDto
{
    public int Precinct { get; set; }
    public int CrimeType { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double PredictedValue { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = "medium";
    public string Trend { get; set; } = "stable";
    public string? Shift { get; set; }
}

public class SpatialPredictionDto
{
    public int Precinct { get; set; }
    public uint ClusterId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double PredictedValue { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = "medium";
    public string Trend { get; set; } = "stable";
}

public class SeasonalPredictionDto
{
    public int Precinct { get; set; }
    public int CrimeType { get; set; }
    public List<double> Trend { get; set; } = new();
    public List<double> Seasonal { get; set; } = new();
    public List<double> Residual { get; set; } = new();
    public Dictionary<string, double> Strength { get; set; } = new();
    public int PeakMonth { get; set; }
    public int TroughMonth { get; set; }
}

public class SaveForecastSnapshotResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalPredictions { get; set; }
    public string RequestedModelType { get; set; } = string.Empty;
    public string EffectiveModelType { get; set; } = string.Empty;
    public string? ModelFallbackReason { get; set; }
    public List<MonthlyForecastRiskPeriod> MonthlyRiskPeriods { get; set; } = new();
    public List<HistoricalDataPointDto> HistoricalData { get; set; } = new();
}
