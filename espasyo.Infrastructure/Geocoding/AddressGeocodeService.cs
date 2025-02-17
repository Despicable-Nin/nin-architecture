using espasyo.Application.Common.Interfaces;

namespace espasyo.Infrastructure.Geocoding;

public class AddressGeocodeService(HttpClient httpClient, ILogger<AddressGeocodeService> logger) : IGeocodeService
{
    public async Task<(double? Latitude, double? Longitude)> GetLatLongAsync(string address)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("espasyo-WebAPI/1.0");
        var requestUrl =
            $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(address)}, Philippines";
        try
        {
            var response = await httpClient.GetFromJsonAsync<NominatimResult[]>(requestUrl);

            if (response?.FirstOrDefault() is { } location && 
                double.TryParse(location.Lat, out var lat) && 
                double.TryParse(location.Lon, out var lon)) 
                return (lat, lon);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while making Nominatim request");
        }
        
        return default;
    }


    public record NominatimResult(string Lat, string Lon, string DisplayName);
}