using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class IncidentGenerator
{
    private static async Task Seed(HttpClient httpClient, SemaphoreSlim semaphore)
    {
        Console.WriteLine("This seed is expected to last for 1000 seconds..");
        Console.WriteLine("Start: {0}", DateTimeOffset.Now);

        const string url = "http://localhost:5041/api/Incident";

     

        for (var i = 1; i <= 5000; i++)
        {
         
            await SendIncidentRequest(url, i, httpClient,semaphore);
            await Task.Delay(1000); // Delay of 1 second between requests
        }

        Console.WriteLine("End: {0}", DateTimeOffset.Now);
    }

    private static async Task SendIncidentRequest(string url, int i, HttpClient client, SemaphoreSlim semaphore)
    {
        var incident = new Request()
        {
            caseId = $"CASE-{i:D4}",
            severity = EnumHelper.GetRandomEnumValue<SeverityEnum>(),
            crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>(),
            motive = EnumHelper.GetRandomEnumValue<MotiveEnum>(),
            precinct = (Barangay)new Random().Next(0,7),
            otherMotive = "xxxxx",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp()
        };

        incident.address = GenerateRandomAddress(incident.precinct);

        var json = JsonSerializer.Serialize(incident);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Wait for the semaphore before sending the request
        await semaphore.WaitAsync();
        try
        {
            var response = await client.PostAsync(url, content);

            Console.WriteLine(response.IsSuccessStatusCode
                ? $"Successfully created incident {incident.caseId}"
                : $"Failed to create incident {incident.caseId}: {response.StatusCode}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string GenerateRandomAddress(Barangay barangay) => AddressGenerator.GenerateRandomAddress(barangay);

    private static string GenerateRandomTimestamp()
    {
        var start = DateTime.UtcNow.AddYears(-1);
        var range = (DateTime.UtcNow - start).Days;
        var randomDate = start.AddDays(new Random().Next(range)).AddHours(new Random().Next(0, 24)).AddMinutes(new Random().Next(0, 60)).AddSeconds(new Random().Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    public static async Task SeedIfNoIncidents(HttpClient httpClient, SemaphoreSlim semaphore)
    {
        Console.WriteLine("Checking for existing incidents...");
        const string url = "http://localhost:5041/api/Incident";

        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            // Parse content as dynamic JSON object
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Try to get totalCount property
            int totalCount = 0;
            if (root.TryGetProperty("totalCount", out var totalCountProp))
            {
                totalCount = totalCountProp.GetInt32();
            }

            if (totalCount > 0)
            {
                Console.WriteLine("Incidents already exist. Seeding aborted.");
                return;
            }
        }
        else
        {
            Console.WriteLine($"Failed to check incidents: {response.StatusCode}. Proceeding with seeding.");
        }

        await Seed(httpClient, semaphore);
    }
    
}