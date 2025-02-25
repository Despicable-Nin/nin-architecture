namespace espasyo.Application.Incidents.Queries.GetGroupedClusters;

public record Filter
{
   public string[] CrimeTypes { get; init; } = [];
   public string[] Motives { get; init; } = [];
   public string[] Severities { get; init; } = [];
   public string[] Weathers { get; init; } = [];
   public string[] Precincts { get; init; } = [];
}

