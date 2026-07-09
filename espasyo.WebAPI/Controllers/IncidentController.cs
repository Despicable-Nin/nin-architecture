using espasyo.Application.Incidents.Commands.CreateIncident;
using espasyo.Application.Incidents.Commands.BulkCreateIncidents;
using espasyo.Application.Incidents.Queries.GetClusters;
using espasyo.Application.Incidents.Queries.GetPaginatedList;
using espasyo.Application.Incidents.Queries.GetIncidentById;
using espasyo.Application.Products.Queries.GetEnums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using espasyo.Application.Incidents.Queries.GetGroupedClusters;
using espasyo.Application.UseCase.Incidents.Commands.ClearIncidents;
using Microsoft.AspNetCore.Authorization;
using espasyo.Application.Common.Models.ML;
using espasyo.Application.UseCase.Incidents.Commands.GenerateStatisticalForecast;
using espasyo.Application.UseCase.Incidents.Commands.ValidateForecastModel;
using espasyo.Application.UseCase.Incidents.Commands.AssessDataQuality;
using espasyo.Application.UseCase.Incidents.Commands.DetectAnomalies;
using espasyo.Application.UseCase.Incidents.Commands.PredictHotspots;

namespace espasyo.WebAPI.Controllers;

//[Authorize]
[Route("api/[controller]")]
[ApiController]
public class IncidentController(IMediator mediator) : ControllerBase
{
        
    // POST api/<IncidentController>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreateIncidentCommand incident)
    {
        var id = await mediator.Send( incident );
        return Created("api/incident", id);
    }

    // POST api/<IncidentController>/bulk
    [HttpPost("bulk")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateIncidentsCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaginated(string search = "", int pageNumber = 1, int pageSize = 10)
    {
        return Ok(await mediator.Send(new GetPaginatedListQuery(search, pageNumber, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetIncidentByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
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

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete()
    {
        await mediator.Send(new ClearIncidentQuery());
        
        return NoContent();
    }

    #region Statistical Forecasting Endpoints

    [HttpPost("forecast/statistical")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateStatisticalForecast([FromBody] StatisticalForecastRequest request)
    {
        try
        {
            var command = new GenerateStatisticalForecastCommand
            {
                ClusterData = request.ClusterData,
                Horizon = request.Horizon,
                ConfidenceLevel = request.ConfidenceLevel,
                ModelType = request.ModelType,
                IncludeSeasonality = request.IncludeSeasonality,
                WeightRecentData = request.WeightRecentData,
                IncludeTimeOfDay = request.IncludeTimeOfDay,
                IncludeMonthOfYear = request.IncludeMonthOfYear,
                IncludeTrend = request.IncludeTrend,
                CrimeTypeFilter = request.CrimeTypeFilter,
                SeverityFilter = request.SeverityFilter,
                CustomThresholds = request.CustomThresholds,
                RiskScoringConfig = request.RiskScoringConfig
            };

            var forecast = await mediator.Send(command);
            return Ok(forecast);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to generate forecast: {ex.Message}");
        }
    }

    [HttpPost("forecast/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateForecastModel([FromBody] StatisticalForecastRequest request)
    {
        try
        {
            var command = new ValidateForecastModelCommand
            {
                ClusterData = request.ClusterData,
                Horizon = request.Horizon,
                ConfidenceLevel = request.ConfidenceLevel,
                ModelType = request.ModelType,
                IncludeSeasonality = request.IncludeSeasonality,
                WeightRecentData = request.WeightRecentData,
                IncludeTimeOfDay = request.IncludeTimeOfDay,
                IncludeMonthOfYear = request.IncludeMonthOfYear,
                IncludeTrend = request.IncludeTrend,
                CrimeTypeFilter = request.CrimeTypeFilter,
                SeverityFilter = request.SeverityFilter
            };

            var validation = await mediator.Send(command);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to validate model: {ex.Message}");
        }
    }

    [HttpPost("forecast/assess-data-quality")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssessDataQuality([FromBody] IEnumerable<ClusterGroup> clusterData)
    {
        try
        {
            var command = new AssessDataQualityCommand
            {
                ClusterData = clusterData
            };

            var assessment = await mediator.Send(command);
            return Ok(assessment);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to assess data quality: {ex.Message}");
        }
    }

    [HttpPost("forecast/hotspots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PredictHotspots([FromBody] PredictHotspotsCommand command)
    {
        try
        {
            var hotspots = await mediator.Send(command);
            return Ok(hotspots);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to predict hotspots: {ex.Message}");
        }
    }

    [HttpPost("anomalies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DetectAnomalies([FromBody] DetectAnomaliesCommand command)
    {
        try
        {
            var anomalies = await mediator.Send(command);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to detect anomalies: {ex.Message}");
        }
    }

    #endregion

}
