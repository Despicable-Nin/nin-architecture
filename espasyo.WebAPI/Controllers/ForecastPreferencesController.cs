using espasyo.Domain.Entities;
using espasyo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace espasyo.WebAPI.Controllers;

[Route("api/forecast/preferences")]
[ApiController]
public class ForecastPreferencesController : ControllerBase
{
    private readonly ApplicationDbContext _sqlServerContext;
    private readonly SqliteApplicationDbContext _sqliteContext;

    public ForecastPreferencesController(
        ApplicationDbContext sqlServerContext,
        SqliteApplicationDbContext sqliteContext)
    {
        _sqlServerContext = sqlServerContext;
        _sqliteContext = sqliteContext;
    }

    private IQueryable<UserForecastPreference> GetContext()
    {
        var dbProvider = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["DatabaseProvider"] ?? "SqlServer";
        return dbProvider.ToLower() == "sqlite"
            ? _sqliteContext.UserForecastPreferences.AsQueryable()
            : _sqlServerContext.UserForecastPreferences.AsQueryable();
    }

    private async Task SaveChangesAsync()
    {
        var dbProvider = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["DatabaseProvider"] ?? "SqlServer";
        if (dbProvider.ToLower() == "sqlite")
            await _sqliteContext.SaveChangesAsync();
        else
            await _sqlServerContext.SaveChangesAsync();
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(string userId)
    {
        var prefs = await GetContext()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            prefs = new UserForecastPreference(userId);
            return Ok(prefs);
        }

        return Ok(prefs);
    }

    [HttpPut("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UpdatePreferencesRequest request)
    {
        var prefs = await GetContext()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            prefs = new UserForecastPreference(userId);
            GetContext().GetType() == typeof(SqliteApplicationDbContext)
                ? _sqliteContext.UserForecastPreferences.Add(prefs)
                : _sqlServerContext.UserForecastPreferences.Add(prefs);
        }

        prefs.DefaultHorizon = request.DefaultHorizon;
        prefs.DefaultConfidenceLevel = request.DefaultConfidenceLevel;
        prefs.DefaultModelType = request.DefaultModelType;
        prefs.ShowEnsembleView = request.ShowEnsembleView;
        prefs.ShowHotspotTimeline = request.ShowHotspotTimeline;
        prefs.EnabledTimeAnimation = request.EnabledTimeAnimation;
        prefs.PreferredTopN = request.PreferredTopN;
        prefs.PreferredPrecincts = request.PreferredPrecincts;
        prefs.PreferredCrimeTypes = request.PreferredCrimeTypes;

        await SaveChangesAsync();
        return Ok(prefs);
    }
}

public record UpdatePreferencesRequest
{
    public int DefaultHorizon { get; init; } = 6;
    public double DefaultConfidenceLevel { get; init; } = 0.95;
    public string DefaultModelType { get; init; } = "SSA";
    public bool ShowEnsembleView { get; init; } = true;
    public bool ShowHotspotTimeline { get; init; } = true;
    public bool EnabledTimeAnimation { get; init; } = false;
    public int PreferredTopN { get; init; } = 10;
    public string? PreferredPrecincts { get; init; }
    public string? PreferredCrimeTypes { get; init; }
}
