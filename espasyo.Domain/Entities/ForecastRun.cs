using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class ForecastRun
{
    protected ForecastRun() { }

    public ForecastRun(
        Guid precinctId,
        int horizon,
        double confidenceLevel,
        ForecastModelTypeEnum modelType,
        string generatedById,
        string name = "",
        ForecastStatusEnum status = ForecastStatusEnum.Draft,
        string? historicalDataJson = null,
        string? requestedModelType = null,
        string? effectiveModelType = null,
        string? modelFallbackReason = null)
    {
        Id = Guid.NewGuid();
        PrecinctId = precinctId;
        RunAt = DateTimeOffset.UtcNow;
        Horizon = horizon;
        ConfidenceLevel = confidenceLevel;
        ModelType = modelType;
        Status = status;
        GeneratedById = generatedById;
        Name = name;
        TotalSeries = 0;
        HistoricalDataJson = historicalDataJson ?? string.Empty;
        RequestedModelType = requestedModelType ?? modelType.ToString();
        EffectiveModelType = effectiveModelType ?? modelType.ToString();
        ModelFallbackReason = modelFallbackReason;
    }

    public Guid Id { get; private set; }
    public Guid PrecinctId { get; private set; }
    public virtual Precinct Precinct { get; set; } = null!;
    public DateTimeOffset RunAt { get; private set; }
    public int Horizon { get; private set; }
    public double ConfidenceLevel { get; private set; }
    public ForecastModelTypeEnum ModelType { get; private set; }
    public string RequestedModelType { get; private set; } = string.Empty;
    public string EffectiveModelType { get; private set; } = string.Empty;
    public string? ModelFallbackReason { get; private set; }
    public string HistoricalDataJson { get; private set; } = string.Empty;
    public ForecastStatusEnum Status { get; private set; }
    public int TotalSeries { get; private set; }
    public string GeneratedById { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public void MarkCompleted(int totalSeries)
    {
        Status = ForecastStatusEnum.Completed;
        TotalSeries = totalSeries;
    }

    public void MarkFailed()
    {
        Status = ForecastStatusEnum.Failed;
    }

    public void Archive()
    {
        Status = ForecastStatusEnum.Archived;
    }
}
