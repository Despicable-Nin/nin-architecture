using espasyo.Application.UseCase.Manpower.Commands.CreateManpower;
using espasyo.Application.UseCase.Manpower.Queries.AnalyzeManpowerNeeds;
using espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;
using espasyo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ManpowerController : ControllerBase
{
    private readonly IMediator _mediator;

    public ManpowerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManpower([FromQuery] int? year, [FromQuery] Barangay? precinct)
    {
        try
        {
            var query = new GetAllManpowerQuery
            {
                Year = year,
                Precinct = precinct
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve manpower data: {ex.Message}");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateManpower([FromBody] CreateManpowerCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);
            return Created($"api/manpower/{id}", new { Id = id });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create manpower allocation: {ex.Message}");
        }
    }

    [HttpPost("analyze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeManpowerNeeds([FromBody] ManpowerAnalysisRequest request)
    {
        try
        {
            var query = new AnalyzeManpowerNeedsQuery
            {
                Year = request.Year,
                PredictedCaseCounts = request.PredictedCaseCounts
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to analyze manpower needs: {ex.Message}");
        }
    }

    [HttpGet("summary/{year}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManpowerSummary(int year)
    {
        try
        {
            var query = new GetAllManpowerQuery { Year = year };
            var manpowers = await _mediator.Send(query);
            
            var summary = new
            {
                Year = year,
                TotalPrecincts = manpowers.Count(),
                TotalManpower = manpowers.Sum(m => m.AllocatedCount),
                AverageAllocation = manpowers.Any() ? manpowers.Average(m => m.AllocatedCount) : 0,
                PrecinctBreakdown = manpowers.GroupBy(m => m.PrecinctName)
                    .Select(g => new
                    {
                        Precinct = g.Key,
                        Allocation = g.Sum(m => m.AllocatedCount)
                    })
                    .OrderByDescending(x => x.Allocation)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve manpower summary: {ex.Message}");
        }
    }
}

public class ManpowerAnalysisRequest
{
    public int Year { get; set; }
    public Dictionary<Barangay, int> PredictedCaseCounts { get; set; } = new();
}