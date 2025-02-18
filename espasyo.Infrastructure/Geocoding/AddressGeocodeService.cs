using espasyo.Application.Common.Interfaces;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace espasyo.Infrastructure.Geocoding
{
    public class AddressGeocodeService : IGeocodeService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<AddressGeocodeService> logger;

        public AddressGeocodeService(HttpClient httpClient, ILogger<AddressGeocodeService> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<(double? Latitude, double? Longitude, string NewAddress)> GetLatLongAsync(string address)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("espasyo-WebAPI/1.0");

            var addressParts = address.Replace(",", "").Split(' ');

            for (var i = 0; i < addressParts.Length; i++)
            {
                var currentAddress = string.Join(" ", addressParts.Skip(i));
                var requestUrl = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(currentAddress)}";

                var coordinates = await GetCoordinatesAsync(requestUrl);

                if (coordinates != null)
                {
                    return (coordinates.Value.Item1, coordinates.Value.Item2,currentAddress);
                }
            }

            return default;
        }

        private async Task<(double, double)?> GetCoordinatesAsync(string requestUrl)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<NominatimResult[]>(requestUrl);

                if (response == null || response.FirstOrDefault() == null)
                {
                    return null;
                }

                if (response.FirstOrDefault() is { } location &&
                    double.TryParse(location.Lat, out var lat) &&
                    double.TryParse(location.Lon, out var lon))
                {
                    return (lat, lon);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while making Nominatim request");
            }

            return null;
        }

        public record NominatimResult(string Lat, string Lon, string DisplayName);
    }
}
