using espasyo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GeocodingController(IGeocodeService geocodeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Geocode([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest("Address is required");

        var (latitude, longitude, formattedAddress) = await geocodeService.GetLatLongAsync(address);

        if (latitude is null || longitude is null)
            return NotFound($"No coordinates found for address: {address}");

        return Ok(new
        {
            latitude = latitude.Value,
            longitude = longitude.Value,
            formattedAddress
        });
    }
}
