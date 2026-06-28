using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class SpatialForecastResult
{
    protected SpatialForecastResult() { }

    public SpatialForecastResult(
        Guid forecastRunId,
        Barangay precinct,
        uint clusterId,
        double? latitude,
        double? longitude,
        int month,
        int year,
        double predictedValue,
        double lowerBound,
        double upperBound,
        double confidence,
        string riskLevel,
        string trend)
    {
        Id = Guid.NewGuid();
        ForecastRunId = forecastRunId;
        Precinct = precinct;
        ClusterId = clusterId;
        Latitude = latitude;
        Longitude = longitude;
        Month = month;
        Year = year;
        PredictedValue = predictedValue;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Confidence = confidence;
        RiskLevel = riskLevel;
        Trend = trend;
    }

    public Guid Id { get; private set; }
    public Guid ForecastRunId { get; private set; }
    public virtual ForecastRun ForecastRun { get; set; } = null!;
    public Barangay Precinct { get; private set; }
    public uint ClusterId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public double PredictedValue { get; private set; }
    public double LowerBound { get; private set; }
    public double UpperBound { get; private set; }
    public double Confidence { get; private set; }
    public string RiskLevel { get; private set; } = string.Empty;
    public string Trend { get; private set; } = string.Empty;
}
