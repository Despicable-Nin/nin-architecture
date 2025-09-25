using espasyo.Application.UseCase.Manpower.Commands.CreateManpower;
using espasyo.Application.UseCase.Manpower.Commands.UpdateManpower;
using espasyo.Application.UseCase.Manpower.Queries.AnalyzeManpowerNeeds;
using espasyo.Application.UseCase.Manpower.Queries.GetAllManpower;
using espasyo.Application.UseCase.Manpower.Queries.GetManpowerById;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using espasyo.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ManpowerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SqliteApplicationDbContext _context;

    public ManpowerController(IMediator mediator, SqliteApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
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

    [HttpPost("upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertManpower([FromBody] UpsertManpowerCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id, Message = "Manpower allocation created or updated successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to upsert manpower allocation: {ex.Message}");
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

    [HttpGet("precinct/{precinctId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetManpowerByPrecinct(Guid precinctId)
    {
        try
        {
            var query = new GetAllManpowerQuery();
            var allManpower = await _mediator.Send(query);
            var precinctManpower = allManpower.Where(m => m.PrecinctId == precinctId).ToList();
            
            if (!precinctManpower.Any())
            {
                return NotFound($"No manpower allocations found for precinct {precinctId}");
            }
            
            return Ok(precinctManpower);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve manpower for precinct: {ex.Message}");
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
                PrecinctId = f.PrecinctId.ToString(),
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
    /// Get all available precincts
    /// </summary>
    [HttpGet("precincts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrecincts()
    {
        try
        {
            var precincts = await _context.Precincts
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Code,
                    p.Population,
                    p.AreaKm2,
                    p.IsActive
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
            
            return Ok(precincts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve precincts: {ex.Message}");
        }
    }

    /// <summary>
    /// Upsert precincts (create if not exists, update if exists)
    /// This is useful for seeding initial precinct data
    /// </summary>
    [HttpPost("precincts/upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertPrecincts([FromBody] UpsertPrecinctsRequest request)
    {
        try
        {
            var upsertedCount = 0;
            var createdCount = 0;
            var updatedCount = 0;

            foreach (var precinctData in request.Precincts)
            {
                // Try to find existing precinct by code
                var existingPrecinct = await _context.Precincts
                    .FirstOrDefaultAsync(p => p.Code == precinctData.Code);

                if (existingPrecinct == null)
                {
                    // Create new precinct
                    var barangay = MapNameToBarangay(precinctData.Name);
                    var newPrecinct = new Precinct(barangay, precinctData.Code);
                    
                    if (precinctData.Population.HasValue || !string.IsNullOrEmpty(precinctData.Description))
                    {
                        newPrecinct.UpdateDetails(barangay, precinctData.Code,
                            precinctData.Population, precinctData.AreaKm2,
                            precinctData.Latitude, precinctData.Longitude,
                            precinctData.Description, precinctData.ContactInfo);
                    }
                    
                    _context.Precincts.Add(newPrecinct);
                    createdCount++;
                }
                else
                {
                    // Update existing precinct
                    var barangay = MapNameToBarangay(precinctData.Name);
                    existingPrecinct.UpdateDetails(barangay, precinctData.Code,
                        precinctData.Population, precinctData.AreaKm2,
                        precinctData.Latitude, precinctData.Longitude,
                        precinctData.Description, precinctData.ContactInfo);
                    
                    updatedCount++;
                }
                
                upsertedCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Successfully processed {upsertedCount} precincts",
                TotalProcessed = upsertedCount,
                Created = createdCount,
                Updated = updatedCount
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to upsert precincts: {ex.Message}");
        }
    }
    
    [HttpGet("shifts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAvailableShifts()
    {
        try
        {
            var shifts = Enum.GetValues<ShiftEnum>()
                .Select(shift => new
                {
                    Value = (int)shift,
                    Name = shift.ToString(),
                    DisplayName = shift switch
                    {
                        ShiftEnum.Morning => "Morning (6:00 AM - 2:00 PM)",
                        ShiftEnum.Evening => "Evening (2:00 PM - 10:00 PM)",
                        ShiftEnum.Night => "Night (10:00 PM - 6:00 AM)",
                        _ => shift.ToString()
                    }
                })
                .ToList();
            
            return Ok(shifts);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve shifts: {ex.Message}");
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
    
    /// <summary>
    /// Map precinct name to Barangay enum
    /// </summary>
    private Barangay MapNameToBarangay(string name)
    {
        return name?.ToLower() switch
        {
            "alabang" => Barangay.Alabang,
            "ayala alabang" => Barangay.Ayala_Alabang,
            "sucat" => Barangay.Sucat,
            "poblacion" => Barangay.Poblacion,
            "putatan" => Barangay.Putatan,
            "tunasan" => Barangay.Tunasan,
            "cupang" => Barangay.Cupang,
            "bayanan" => Barangay.Bayanan,
            "buli" => Barangay.Buli,
            _ => Barangay.Alabang // Default fallback
        };
    }
}

public class ManpowerAnalysisRequest
{
    public int Year { get; set; }
    public Dictionary<Guid, int> PredictedCaseCounts { get; set; } = new();
}

public class DynamicAllocationRequest
{
    public List<ForecastData> Forecasts { get; set; } = new();
}

public class ForecastData
{
    public Guid PrecinctId { get; set; }
    public int PredictedCount { get; set; }
}

public class UpsertPrecinctsRequest
{
    public List<PrecinctData> Precincts { get; set; } = new();
}

public class PrecinctData
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int? Population { get; set; }
    public decimal? AreaKm2 { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public string? ContactInfo { get; set; }
}
