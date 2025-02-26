using espasyo.Application.Incidents.Commands.CreateIncident;
using espasyo.Application.Incidents.Queries.GetClusters;
using espasyo.Application.Incidents.Queries.GetPaginatedList;
using espasyo.Application.Products.Queries.GetEnums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using espasyo.Application.Incidents.Queries.GetGroupedClusters;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IncidentController( IMediator mediator) : ControllerBase
{
        
    // POST api/<IncidentController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreateIncidentCommand incident)
    {
        var id = await mediator.Send( incident );
        return Created("api/incident", id);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaginated(string search = "", int pageNumber = 1, int pageSize = 10)
    {
        return Ok(await mediator.Send(new GetPaginatedListQuery(search, pageNumber, pageSize)));
    }
    
    [HttpGet("enums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEnums([FromQuery] string name)
    {
        try
        {
            var result = await mediator.Send(new GetEnumsQuery() { Name = name });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }                   
    }

    [HttpPut("clusters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateClusters(GetClustersQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPut("grouped-clusters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateClusters(GetGroupedClustersQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(result);
    }

   

}