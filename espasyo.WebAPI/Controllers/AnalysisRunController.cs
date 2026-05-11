using espasyo.Application.Interfaces;
using espasyo.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/analysis-runs")]
[ApiController]
public class AnalysisRunController : ControllerBase
{
    private readonly IAnalysisRunRepository _analysisRunRepo;

    public AnalysisRunController(IAnalysisRunRepository analysisRunRepo)
    {
        _analysisRunRepo = analysisRunRepo;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAnalysisRunRequest request)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system"
                : "system";

            var run = new AnalysisRun(
                request.ParametersJson,
                request.ClusterGroupsJson,
                request.QualityMetricsJson,
                userId);

            var saved = await _analysisRunRepo.SaveAsync(run);
            return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var runs = await _analysisRunRepo.GetAllAsync(page, pageSize);
            return Ok(new { Page = page, PageSize = pageSize, Items = runs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var run = await _analysisRunRepo.GetByIdAsync(id);
            if (run == null)
                return NotFound($"Analysis run {id} not found");

            return Ok(run);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _analysisRunRepo.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Analysis run {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public record CreateAnalysisRunRequest(
    string ParametersJson,
    string ClusterGroupsJson,
    string QualityMetricsJson);
