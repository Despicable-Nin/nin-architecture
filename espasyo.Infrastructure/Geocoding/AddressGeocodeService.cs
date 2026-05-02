using System.Net.Http.Json;
using espasyo.Application.Interfaces;

namespace espasyo.Infrastructure.Geocoding
{
    public class AddressGeocodeService(
        HttpClient httpClient,
        ILogger<AddressGeocodeService> logger)
        : IGeocodeService
    {
        // Global request gate
        private static readonly SemaphoreSlim RequestGate = new(1, 1);
        private static DateTime _lastRequestUtc = DateTime.MinValue;

        // Nominatim recommends low request rates
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);

        public async Task<(double? Latitude, double? Longitude, string NewAddress)> GetLatLongAsync(string address)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("espasyo-WebAPI/1.0");

            var addressParts = address.Replace(",", "").Split(' ');

            for (var i = 0; i < addressParts.Length; i++)
            {
                var currentAddress = string.Join(" ", addressParts.Skip(i));
                var requestUrl =
                    $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(currentAddress)}";

                var coordinates = await GetCoordinatesAsync(requestUrl);

                if (coordinates != null)
                {
                    return (coordinates.Value.Item1, coordinates.Value.Item2, currentAddress);
                }
            }

            return default;
        }

        private async Task<(double, double)?> GetCoordinatesAsync(string requestUrl)
        {
            await RequestGate.WaitAsync();

            try
            {
                var elapsed = DateTime.UtcNow - _lastRequestUtc;

                if (elapsed < MinimumInterval)
                {
                    var delay = MinimumInterval - elapsed;
                    logger.LogInformation("Rate guard active. Waiting {DelayMs}ms", delay.TotalMilliseconds);

                    await Task.Delay(delay);
                }

                _lastRequestUtc = DateTime.UtcNow;

                var response =
                    await httpClient.GetFromJsonAsync<NominatimResult[]>(requestUrl);

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
            finally
            {
                RequestGate.Release();
            }

            return null;
        }

        public record NominatimResult(string Lat, string Lon, string DisplayName);
    }
}