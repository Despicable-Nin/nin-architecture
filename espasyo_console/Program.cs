using System.Text;
using System.Text.Json;
using static System.Console;

namespace espasyo_console;

public class Program
{
    private static readonly HttpClient Client = new HttpClient();
    private static readonly Random Random = new Random();
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1); // Allow only 1 concurrent request

    private static async Task Main(string[] args)
    {
        WriteLine("This seed is expected to last for 1000 seconds..");
        WriteLine("Start: {0}", DateTimeOffset.Now);

        const string url = "http://localhost:5041/api/Incident";

        for (var i = 1; i <= 1000; i++)
        {
            await SendIncidentRequest(url, i);
            await Task.Delay(1000); // Delay of 1 second between requests
        }

        WriteLine("End: {0}", DateTimeOffset.Now);
        WriteLine("Press any key to exit...");
    }

    private static async Task SendIncidentRequest(string url, int i)
    {
        var incident = new
        {
            caseId = $"CASE-{i:D4}",
            address = GenerateRandomAddress(),
            severity = Random.Next(1, 4),
            crimeType = Random.Next(1, 4),
            motive = Random.Next(1, 4),
            policeDistrict = Random.Next(1, 4),
            otherMotive = "string",
            weather = Random.Next(1, 4),
            timeStamp = GenerateRandomTimestamp()
        };

        var json = JsonSerializer.Serialize(incident);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Wait for the semaphore before sending the request
        await Semaphore.WaitAsync();
        try
        {
            var response = await Client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                WriteLine($"Successfully created incident {incident.caseId}");
            }
            else
            {
                WriteLine($"Failed to create incident {incident.caseId}: {response.StatusCode}");
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    static string GenerateRandomAddress()
    {
        string[] streets = { "Block 1 Purok 1", "Block 2 Purok 2", "Block 3 Purok 3", "Block 4 Purok 4", "Block 5 Purok 5" }; // Example streets
        string[] barangays = { "Brgy. Alabang", "Brgy. Bayanan", "Brgy. Putatan", "Brgy. Poblacion", "Brgy. Tunasan" };
        string[] postalCodes = { "1770", "1771", "1772", "1773", "1774" }; // Example postal codes for Muntinlupa City

        var street = streets[Random.Next(streets.Length)];
        var barangay = barangays[Random.Next(barangays.Length)];
        var postalCode = postalCodes[Random.Next(postalCodes.Length)];

        return $"{Random.Next(1, 1000)} {street}, {barangay}, Muntinlupa City, {postalCode}, Philippines";
    }

    static string GenerateRandomTimestamp()
    {
        var start = DateTime.UtcNow.AddYears(-1);
        var range = (DateTime.UtcNow - start).Days;
        var randomDate = start.AddDays(Random.Next(range)).AddHours(Random.Next(0, 24)).AddMinutes(Random.Next(0, 60)).AddSeconds(Random.Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
