using espasyo.Application.Interfaces;
using espasyo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PipelineController : ControllerBase
{
    private readonly PipelineOrchestratorService _pipeline;
    private readonly IManpowerRecommendationRepository _manpowerRecommendationRepo;
    private readonly IForecastRepository _forecastRepo;

    public PipelineController(
        PipelineOrchestratorService pipeline,
        IManpowerRecommendationRepository manpowerRecommendationRepo,
        IForecastRepository forecastRepo)
    {
        _pipeline = pipeline;
        _manpowerRecommendationRepo = manpowerRecommendationRepo;
        _forecastRepo = forecastRepo;
    }

    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunPipeline([FromBody] PipelineRequest request)
    {
        try
        {
            var result = await _pipeline.RunFullPipeline(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "Pipeline execution failed",
                Detail = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("recommendations/{forecastRunId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations(Guid forecastRunId)
    {
        try
        {
            var run = await _forecastRepo.GetForecastRunByIdAsync(forecastRunId);
            if (run == null)
                return NotFound($"Forecast run {forecastRunId} not found");

            var recommendations = await _manpowerRecommendationRepo.GetByForecastRunIdAsync(forecastRunId);
            return Ok(new
            {
                ForecastRunId = forecastRunId,
                GeneratedAt = run.RunAt,
                Recommendations = recommendations.Select(r => new
                {
                    r.Id,
                    PrecinctId = r.PrecinctId,
                    PrecinctName = r.Precinct.Name,
                    Shift = r.Shift.ToString(),
                    r.RecommendedHeadCount,
                    r.PredictedWorkloadHours,
                    r.ComplexityScore,
                    r.Confidence,
                    r.Justification,
                    r.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}
