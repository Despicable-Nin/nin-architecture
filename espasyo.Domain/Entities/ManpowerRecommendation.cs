using espasyo.Domain.Enums;

namespace espasyo.Domain.Entities;

public class ManpowerRecommendation
{
    protected ManpowerRecommendation() { }

    public ManpowerRecommendation(
        Guid forecastRunId,
        Guid precinctId,
        ShiftEnum shift,
        int recommendedHeadCount,
        float predictedWorkloadHours,
        float complexityScore,
        float confidence,
        string justification)
    {
        Id = Guid.NewGuid();
        ForecastRunId = forecastRunId;
        PrecinctId = precinctId;
        Shift = shift;
        RecommendedHeadCount = recommendedHeadCount;
        PredictedWorkloadHours = predictedWorkloadHours;
        ComplexityScore = complexityScore;
        Confidence = confidence;
        Justification = justification;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ForecastRunId { get; private set; }
    public virtual ForecastRun ForecastRun { get; set; } = null!;
    public Guid PrecinctId { get; private set; }
    public virtual Precinct Precinct { get; set; } = null!;
    public ShiftEnum Shift { get; private set; }
    public int RecommendedHeadCount { get; private set; }
    public float PredictedWorkloadHours { get; private set; }
    public float ComplexityScore { get; private set; }
    public float Confidence { get; private set; }
    public string Justification { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
