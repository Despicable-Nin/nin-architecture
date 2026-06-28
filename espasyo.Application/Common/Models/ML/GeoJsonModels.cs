namespace espasyo.Application.Common.Models.ML;

public record GeoJsonFeatureCollection
{
    public string Type => "FeatureCollection";
    public List<GeoJsonFeature> Features { get; init; } = [];
}

public record GeoJsonFeature
{
    public string Type => "Feature";
    public GeoJsonGeometry Geometry { get; init; } = null!;
    public Dictionary<string, object> Properties { get; init; } = [];
}

public record GeoJsonGeometry
{
    public string Type { get; init; } = "Polygon";
    public List<List<List<double>>> Coordinates { get; init; } = [];
}

public record HotspotPredictionRequest
{
    public IEnumerable<ClusterGroup> ClusterData { get; init; } = [];
    public int Horizon { get; init; } = 6;
    public double ConfidenceLevel { get; init; } = 0.95;
    public string ModelType { get; init; } = "Linear";
    public bool IncludeSeasonality { get; init; } = true;
    public bool WeightRecentData { get; init; } = true;
    public bool IncludeTimeOfDay { get; init; } = false;
    public bool IncludeMonthOfYear { get; init; } = false;
    public bool IncludeTrend { get; init; } = true;
    public string[]? CrimeTypeFilter { get; init; }
    public string[]? SeverityFilter { get; init; }
    public double HotspotThreshold { get; init; } = 0.7;
}
