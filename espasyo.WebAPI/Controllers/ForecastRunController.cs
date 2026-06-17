using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastRun;
using espasyo.Application.UseCase.ForecastRuns.Commands.SaveForecastSnapshot;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastResults;
using espasyo.Application.UseCase.ForecastRuns.Queries.GetForecastRuns;
using espasyo.Application.UseCase.ForecastRuns.Queries.EvaluateForecastRun;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ForecastRunController : ControllerBase
{
    private readonly IMediator _mediator;

    public ForecastRunController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveForecastRun([FromBody] SaveForecastRunCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);
            return Created($"api/forecastrun/{id}", new { Id = id });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to save forecast run: {ex.Message}");
        }
    }

    [HttpPost("snapshot")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveForecastSnapshotResponse>> SaveSnapshot(
        [FromBody] SaveForecastSnapshotCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return Created($"api/forecastrun/{result.Id}", result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to save forecast snapshot: {ex.Message}");
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForecastRuns([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetForecastRunsQuery { Page = page, PageSize = pageSize };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForecastResults(Guid id)
    {
        var query = new GetForecastResultsQuery { ForecastRunId = id };
        var results = await _mediator.Send(query);

        if (!results.Any())
            return NotFound($"No results found for forecast run {id}");

        return Ok(results);
    }

    [HttpGet("{id:guid}/evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluateForecastRun(Guid id)
    {
        try
        {
            var query = new EvaluateForecastRunQuery { ForecastRunId = id };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to evaluate forecast run: {ex.Message}");
        }
    }
}
