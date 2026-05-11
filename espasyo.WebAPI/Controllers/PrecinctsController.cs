using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PrecinctsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions;

    private static readonly Dictionary<string, string> PrecinctFileMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ALB"] = "Alabang.json",
        ["AAL"] = "Ayala_Alabang.json",
        ["SUC"] = "Sucat.json",
        ["POB"] = "Poblacion.json",
        ["PUT"] = "Putatan.json",
        ["TUN"] = "Tunasan.json",
        ["CUP"] = "Cupang.json",
        ["BAY"] = "Bayanan.json",
        ["BUL"] = "Buli.json",
    };

    private static readonly Dictionary<string, string> CodeToName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ALB"] = "Alabang",
        ["AAL"] = "Ayala Alabang",
        ["SUC"] = "Sucat",
        ["POB"] = "Poblacion",
        ["PUT"] = "Putatan",
        ["TUN"] = "Tunasan",
        ["CUP"] = "Cupang",
        ["BAY"] = "Bayanan",
        ["BUL"] = "Buli",
    };

    public PrecinctsController(IWebHostEnvironment env)
    {
        _env = env;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private string GetStreetsDirectory()
    {
        var path = Path.Combine(_env.ContentRootPath, "Data", "Streets");
        if (!Directory.Exists(path))
        {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Streets");
        }
        return path;
    }

    [HttpGet("streets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPrecinctStreets()
    {
        try
        {
            var streetsDir = GetStreetsDirectory();
            var result = new List<object>();

            foreach (var kvp in PrecinctFileMap)
            {
                var filePath = Path.Combine(streetsDir, kvp.Value);
                if (!System.IO.File.Exists(filePath))
                {
                    result.Add(new
                    {
                        Code = kvp.Key,
                        Name = CodeToName[kvp.Key],
                        StreetCount = 0,
                        Streets = Array.Empty<string>()
                    });
                    continue;
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var streets = JsonSerializer.Deserialize<List<StreetEntry>>(json, _jsonOptions);
                var streetNames = streets?.Select(s => s.Street).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();

                result.Add(new
                {
                    Code = kvp.Key,
                    Name = CodeToName[kvp.Key],
                    StreetCount = streetNames.Count,
                    Streets = streetNames
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve precinct streets: {ex.Message}");
        }
    }

    [HttpGet("{code}/streets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrecinctStreets(string code)
    {
        try
        {
            var codeUpper = code.ToUpperInvariant();
            if (!PrecinctFileMap.TryGetValue(codeUpper, out var fileName))
            {
                return NotFound($"Precinct with code '{code}' not found. Valid codes: {string.Join(", ", PrecinctFileMap.Keys)}");
            }

            var streetsDir = GetStreetsDirectory();
            var filePath = Path.Combine(streetsDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Street data for precinct '{code}' not found");
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var streets = JsonSerializer.Deserialize<List<StreetEntry>>(json, _jsonOptions);
            var streetNames = streets?.Select(s => s.Street).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();

            return Ok(new
            {
                Code = codeUpper,
                Name = CodeToName[codeUpper],
                StreetCount = streetNames.Count,
                Streets = streetNames
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to retrieve streets for precinct '{code}': {ex.Message}");
        }
    }

    private record StreetEntry(string Street);
}
