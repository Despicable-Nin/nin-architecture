namespace espasyo.Application.Common.Models.ML;

public record TrainerModel
{
   public string? CaseId { get; init; }
   public int CrimeType { get; init; }
   public string? TimeStamp { get; init; }
   public string? Address { get; init; }
   public double Latitude { get; init; }
   public double Longitude { get; init; }
   public int Severity { get; init; } 
   public int PoliceDistrict { get; init; } 
   public int Weather { get; init; } 
   public int CrimeMotive { get; init; }
}

public record ClusteredModel : TrainerModel
{
   public int? ClusterId { get; init; }
}

public class LatLong
{
   public float Latitude { get; set; }
   public float Longitude { get; set; }
}