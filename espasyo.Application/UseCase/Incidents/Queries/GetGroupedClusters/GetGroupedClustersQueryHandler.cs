using espasyo.Application.Common.Models.ML;
using espasyo.Application.Extensions;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetGroupedClusters;

public class GetGroupedClustersQueryHandler(
    ILogger<GetGroupedClustersQueryHandler> logger,
    IMachineLearningService kMeansService,
    IIncidentRepository repository) : IRequestHandler<GetGroupedClustersQuery, GroupedClusterResponse>
{

    public async Task<GroupedClusterResponse> Handle(GetGroupedClustersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching incident records...");

        KeyValuePair<DateOnly, DateOnly>? dateRange = null;
        if (request is { DateFrom: not null, DateTo: not null })
            dateRange = new KeyValuePair<DateOnly, DateOnly>(request.DateFrom.Value, request.DateTo.Value);

        // Fetch and project directly to TrainerModel to avoid extra allocations
        var incidents = await repository.GetFilteredIncidentsAsync(
            dateRange,
            request.Filters.CrimeTypes,
            request.Filters.Motives,
            request.Filters.Weathers,
            request.Filters.Precincts,
            request.Filters.Severities
        ).ConfigureAwait(false);

        // Materialize to array for fast indexed access and to avoid multiple enumerations
        var trainerModels = incidents.Select(x => new TrainerModel
        {
            Address = x.Address,
            Latitude = x.GetLatitude(),
            Longitude = x.GetLongitude(),
            Severity = (int)x.Severity,
            Weather = (int)x.Weather,
            CaseId = x.CaseId,
            Motive = (int)x.Motive,
            CrimeType = (int)x.CrimeType,
            PoliceDistrict = (int)x.PoliceDistrict,
            TimeStamp = x.TimeStamp.ToString(),
            TimeStampUnix = x.TimeStamp!.Value.ToUnixTimeSeconds()
        }).ToArray();

        var result = kMeansService.PerformKMeansAndGetGroupedClusters(trainerModels, request.Features, request.NumberOfClusters, request.NumberOfRuns);

        // Build dictionary with capacity for better performance
        var trainerModelDict = new Dictionary<string, TrainerModel>(trainerModels.Length);
        foreach (var model in trainerModels)
        {
            if (model.CaseId != null)
                trainerModelDict[model.CaseId] = model;
        }

        // Use local DateTimeKind and cache epoch for performance
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var r in result.ClusterGroups)
        {
            foreach (var item in r.ClusterItems)
            {
                if (item.CaseId != null && trainerModelDict.TryGetValue(item.CaseId, out var trainerData))
                {
                    var date = epoch.AddSeconds(trainerData.TimeStampUnix);
                    item.Month = date.Month;
                    item.Year = date.Year;
                    item.TimeOfDay = date.GetTimeOfDay();
                    item.Precinct = (Barangay)trainerData.PoliceDistrict;
                }
            }
        }

        return result;
    }
}