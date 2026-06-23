using MediatR;

namespace espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;

public class SaveForecastSnapshotCommand : IRequest<SaveForecastSnapshotResponse>
{
    public string Name { get; set; } = string.Empty;
    public int ForecastPeriod { get; set; }
    public double ConfidenceLevel { get; set; } = 0.95;
    public List<PredictionDto> Predictions { get; set; } = new();
    public string GeneratedById { get; set; } = string.Empty;
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
}

public class SaveForecastSnapshotResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalPredictions { get; set; }
}
