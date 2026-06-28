namespace espasyo.Domain.Entities;

public class UserForecastPreference
{
    protected UserForecastPreference() { }

    public UserForecastPreference(string userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DefaultHorizon = 6;
        DefaultConfidenceLevel = 0.95;
        DefaultModelType = "Linear";
        ShowEnsembleView = true;
        ShowHotspotTimeline = true;
        EnabledTimeAnimation = false;
        PreferredTopN = 10;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int DefaultHorizon { get; set; }
    public double DefaultConfidenceLevel { get; set; }
    public string DefaultModelType { get; set; } = string.Empty;
    public bool ShowEnsembleView { get; set; }
    public bool ShowHotspotTimeline { get; set; }
    public bool EnabledTimeAnimation { get; set; }
    public int PreferredTopN { get; set; }
    public string? PreferredPrecincts { get; set; }
    public string? PreferredCrimeTypes { get; set; }
}
