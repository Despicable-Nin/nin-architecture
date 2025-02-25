using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
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
        
        if(request is { DateFrom: not null, DateTo: not null }) dateRange = new KeyValuePair<DateOnly, DateOnly>(request.DateFrom.Value, request.DateTo.Value); 

        var incidents = await repository.GetFilteredIncidentsAsync(
            dateRange, 
            request.Filters.CrimeTypes, 
            request.Filters.Motives,
            request.Filters.Weathers, 
            request.Filters.Precincts, 
            request.Filters.Severities
        );

        //map to trainerModel
        var trainerModels = incidents.Select(x => new TrainerModel
        {
            Address = x.Address,
            Latitude = x.GetLatitude(),
            Longitude = x.GetLongitude(),
            Severity = (int)x.Severity,
            Weather = (int)x.Weather,
            CaseId = x.CaseId,
            CrimeMotive = (int)x.Motive,
            CrimeType = (int)x.CrimeType,
            PoliceDistrict = (int)x.PoliceDistrict,
            TimeStamp = x.TimeStamp.ToString(),
            TimeStampUnix = x.TimeStamp!.Value.ToUnixTimeSeconds()
        });

        var result = kMeansService.PerformKMeansAndGetGroupedClusters(trainerModels, request.Features, request.NumberOfClusters, request.NumberOfRuns);

        return result;
    }
}