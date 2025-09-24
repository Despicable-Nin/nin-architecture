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
