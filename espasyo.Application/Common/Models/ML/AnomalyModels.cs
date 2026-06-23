namespace espasyo.Application.Common.Models.ML;

public record AnomalyDetectionRequest
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string Method { get; init; } = "all";
    public string GroupBy { get; init; } = "month";
}

public record AnomalyResult
{
    public string Precinct { get; init; } = string.Empty;
    public string CrimeType { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public double ActualCount { get; init; }
    public double ExpectedCount { get; init; }
    public double Deviation { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public List<string> ContributingFactors { get; init; } = [];
}

public record AnomalyDetectionResponse
{
    public List<AnomalyResult> Anomalies { get; init; } = [];
    public int TotalAnomalies { get; init; }
    public int TotalDataPoints { get; init; }
    public double AnomalyRate { get; init; }
}
