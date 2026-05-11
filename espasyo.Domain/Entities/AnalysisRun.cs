namespace espasyo.Domain.Entities;

public class AnalysisRun
{
    protected AnalysisRun() { }

    public AnalysisRun(
        string parametersJson,
        string clusterGroupsJson,
        string qualityMetricsJson,
        string createdById)
    {
        Id = Guid.NewGuid();
        ParametersJson = parametersJson;
        ClusterGroupsJson = clusterGroupsJson;
        QualityMetricsJson = qualityMetricsJson;
        CreatedById = createdById;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ParametersJson { get; private set; } = string.Empty;
    public string ClusterGroupsJson { get; private set; } = string.Empty;
    public string QualityMetricsJson { get; private set; } = string.Empty;
    public string CreatedById { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
