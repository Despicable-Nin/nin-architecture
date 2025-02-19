using Microsoft.ML.Data;

namespace espasyo.Application.Common.Models.ML;

public class ClusterPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }

    [ColumnName("CaseID")]
    public string CaseId { get; set; }
    [ColumnName("Latitude")]
    public string Latitude { get; set; }
    [ColumnName("Longitude")]
    public string Longitude { get; set; }
}