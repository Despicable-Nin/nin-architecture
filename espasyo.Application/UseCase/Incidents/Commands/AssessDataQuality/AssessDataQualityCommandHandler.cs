using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using MediatR;

namespace espasyo.Application.UseCase.Incidents.Commands.AssessDataQuality;

public class AssessDataQualityCommandHandler(
    IMachineLearningService machineLearningService,
    ILogger<AssessDataQualityCommandHandler> logger
) : IRequestHandler<AssessDataQualityCommand, DataQualityAssessment>
{
    public async Task<DataQualityAssessment> Handle(AssessDataQualityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing data quality assessment request with {ClusterCount} clusters", 
                request.ClusterData?.Count() ?? 0);

            // Validate input
            if (request.ClusterData == null || !request.ClusterData.Any())
            {
                return new DataQualityAssessment
                {
                    IsValid = false,
                    DataPoints = 0,
                    Issues = new List<string> { "No cluster data provided for quality assessment" },
                    Recommendations = new List<string> { "Please run clustering analysis first to generate data for quality assessment" }
                };
            }

            // Assess data quality using ML service
            var assessment = await machineLearningService.AssessDataQuality(request.ClusterData);

            logger.LogInformation("Data quality assessment completed. Valid: {IsValid}, Data points: {DataPoints}, Outliers: {OutlierCount}", 
                assessment.IsValid, assessment.DataPoints, assessment.OutlierCount);

            return assessment;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assessing data quality");
            throw;
        }
    }
}
