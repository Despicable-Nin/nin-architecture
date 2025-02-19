namespace espasyo.Application.Common.Models.ML;

public class ClusterResult
{
    public uint ClusterId { get; set; }
    public List<LatLong> Incident { get; set; }
}