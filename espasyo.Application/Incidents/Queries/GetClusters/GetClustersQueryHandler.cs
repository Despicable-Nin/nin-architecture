using espasyo.Application.Common.Interfaces;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.Incidents.Queries.GetClusters;

public class GetClustersQueryHandler(
    ILogger<GetClustersQueryHandler> logger,
    IMachineLearningService kMeansService,
    IIncidentRepository repository) : IRequestHandler<GetClustersQuery, GetClustersResult>
{

    public async Task<GetClustersResult> Handle(GetClustersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching incident records...");

        KeyValuePair<DateOnly, DateOnly>? dateRange = null;
        
        if(request is { DateFrom: not null, DateTo: not null }) dateRange = new KeyValuePair<DateOnly, DateOnly>(request.DateFrom.Value, request.DateTo.Value); 

        var incidents = await repository.GetAllIncidentsAsync(dateRange);

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
            TimeStamp = x.TimeStamp.ToString()
        });

        var clusteredModels = kMeansService.PerformKMeansClustering(trainerModels, request.Features, request.NumberOfClusters, request.NumberOfRuns);

        return new GetClustersResult(clusteredModels);
    }
}