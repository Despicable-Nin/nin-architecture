using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class SeasonalDecompositionResult
{
    protected SeasonalDecompositionResult() { }

    public SeasonalDecompositionResult(
        Guid forecastRunId,
        Barangay precinct,
        CrimeTypeEnum crimeType,
        string trendData,
        string seasonalData,
        string residualData,
        double strengthTrend,
        double strengthSeasonal,
        int peakMonth,
        int troughMonth)
    {
        Id = Guid.NewGuid();
        ForecastRunId = forecastRunId;
        Precinct = precinct;
        CrimeType = crimeType;
        TrendData = trendData;
        SeasonalData = seasonalData;
        ResidualData = residualData;
        StrengthTrend = strengthTrend;
        StrengthSeasonal = strengthSeasonal;
        PeakMonth = peakMonth;
        TroughMonth = troughMonth;
    }

    public Guid Id { get; private set; }
    public Guid ForecastRunId { get; private set; }
    public virtual ForecastRun ForecastRun { get; set; } = null!;
    public Barangay Precinct { get; private set; }
    public CrimeTypeEnum CrimeType { get; private set; }
    public string TrendData { get; private set; } = string.Empty;
    public string SeasonalData { get; private set; } = string.Empty;
    public string ResidualData { get; private set; } = string.Empty;
    public double StrengthTrend { get; private set; }
    public double StrengthSeasonal { get; private set; }
    public int PeakMonth { get; private set; }
    public int TroughMonth { get; private set; }
}
