using Microsoft.ML.Data;

namespace espasyo.Application.Common.Models.ML;

public record TrainerModel
{
   public string? CaseId { get; init; }
   public int CrimeType { get; init; }
   public string? TimeStamp { get; init; }
   public long TimeStampUnix { get; init; }
   public string? Address { get; init; }
   public double Latitude { get; init; }
   public double Longitude { get; init; }
   public int Severity { get; init; } 
   public int PoliceDistrict { get; init; } 
   public int Weather { get; init; } 
   public int Motive { get; init; }
}

public record ClusteredModel
{
   public string CaseId { get; set; }

   // This maps the ML.NET output column "PredictedLabel" to this property.
   [ColumnName("PredictedLabel")]
   public uint ClusterId { get; set; }

   public double Latitude { get; set; }
   public double Longitude { get; set; }
}

public class ClusterItem
{
   public string CaseId { get; set; }
   public double Latitude { get; set; }
   public double Longitude { get; set; }
}

public record ClusterGroup
{
    public uint ClusterId { get; set; }
    public float[] Centroids => ClusterItems.Count != 0
       ?
         [
             (float)ClusterItems.Average(item => item.Latitude),
             (float)ClusterItems.Average(item => item.Longitude)
         ]
       : [0f, 0f];
    public List<ClusterItem> ClusterItems { get; set; } = [];
    public int ClusterCount => ClusterItems.Count;
}

public record GroupedClusterResponse
{
   public IEnumerable<ClusterGroup> ClusterGroups { get; init; } = [];
   public IEnumerable<string> Filters { get; init; } = [];
}