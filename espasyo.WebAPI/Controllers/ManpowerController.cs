using espasyo.Application.UseCase.Manpower.Commands.CreateManpower;
using espasyo.Application.UseCase.Manpower.Commands.UpdateManpower;
using espasyo.Application.UseCase.Manpower.Queries.AnalyzeManpowerNeeds;
using espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;
using espasyo.Application.UseCase.Manpower.Queries.GetManpowerById;
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
    public async Task<IActionResult> GetManpower()
    {
        try
        {
            var query = new GetAllManpowerQuery();
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManpowerById(Guid id)
    {
        try
        {
            var query = new GetManpowerByIdQuery { Id = id };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound($"Manpower allocation with ID {id} not found");
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve manpower allocation: {ex.Message}");
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateManpower(Guid id, [FromBody] UpdateManpowerCommand command)
    {
        try
        {
            command.Id = id; // Ensure the ID from the route is used
            var success = await _mediator.Send(command);
            
            if (!success)
            {
                return NotFound($"Manpower allocation with ID {id} not found");
            }
            
            return Ok(new { Id = id, Message = "Manpower allocation updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update manpower allocation: {ex.Message}");
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
                PredictedCaseCounts = new Dictionary<Guid, int>() // Simplified - empty for now
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to analyze manpower needs: {ex.Message}");
        }
    }

    [HttpPost("allocate-dynamic")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateDynamicAllocation([FromBody] DynamicAllocationRequest request)
    {
        try
        {
            // This would integrate with the new DynamicManpowerAllocationService
            // For now, return a simplified calculation based on the request data
            var results = request.Forecasts.Select(f => new 
            {
                Precinct = f.Precinct.ToString(),
                PredictedCrimes = f.PredictedCount,
                RecommendedManpower = CalculateSimplifiedAllocation(f.PredictedCount),
                WorkloadLevel = DetermineWorkloadLevel(f.PredictedCount),
                Justification = $"Recommended {CalculateSimplifiedAllocation(f.PredictedCount)} officers for {f.PredictedCount} predicted crimes based on dynamic analysis."
            }).ToList();
            
            return Ok(new { Recommendations = results });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to calculate dynamic allocation: {ex.Message}");
        }
    }
    
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManpowerSummary()
    {
        try
        {
            var query = new GetAllManpowerQuery();
            var manpowers = await _mediator.Send(query);
            
            var summary = new
            {
                TotalPrecincts = manpowers.Count(),
                TotalManpower = manpowers.Sum(m => m.HeadCount),
                AverageAllocation = manpowers.Any() ? manpowers.Average(m => m.HeadCount) : 0,
                PrecinctBreakdown = manpowers.GroupBy(m => m.PrecinctName)
                    .Select(g => new
                    {
                        Precinct = g.Key,
                        HeadCount = g.Sum(m => m.HeadCount),
                        Status = g.First().Status,
                        Variance = g.First().Variance
                    })
                    .OrderByDescending(x => x.HeadCount)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve manpower summary: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Simple allocation formula: Base of 2 officers + 1 officer per 10 crimes
    /// This is a simplified version - real implementation would use historical data
    /// </summary>
    private int CalculateSimplifiedAllocation(int predictedCrimes)
    {
        const int baseOfficers = 2;  // Minimum staffing per precinct
        const double crimesPerOfficer = 10.0;  // Base ratio (would be calculated from data)
        
        var calculatedOfficers = baseOfficers + (int)Math.Ceiling(predictedCrimes / crimesPerOfficer);
        return Math.Max(baseOfficers, Math.Min(calculatedOfficers, 20)); // Cap at 20 officers max
    }
    
    /// <summary>
    /// Get all available precincts/barangays
    /// </summary>
    [HttpGet("precincts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPrecincts()
    {
        try
        {
            var precincts = Enum.GetValues<Barangay>()
                .Select(b => new { 
                    Value = (int)b, 
                    Name = b.ToString().Replace('_', ' ') 
                })
                .OrderBy(p => p.Value)
                .ToList();
            
            return Ok(precincts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve precincts: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Determine workload level based on crime count
    /// In real implementation, this would use historical percentiles
    /// </summary>
    private string DetermineWorkloadLevel(int crimeCount)
    {
        return crimeCount switch
        {
            <= 15 => "Light",
            <= 30 => "Normal", 
            <= 50 => "Heavy",
            _ => "Critical"
        };
    }
}

public class ManpowerAnalysisRequest
{
    public int Year { get; set; }
    public Dictionary<Barangay, int> PredictedCaseCounts { get; set; } = new();
}

public class DynamicAllocationRequest
{
    public List<ForecastData> Forecasts { get; set; } = new();
}

public class ForecastData
{
    public Barangay Precinct { get; set; }
    public int PredictedCount { get; set; }
}
