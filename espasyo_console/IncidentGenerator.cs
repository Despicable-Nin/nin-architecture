using System.Text;
using System.Text.Json;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class IncidentGenerator
{
    public static async Task Seed(HttpClient httpClient, SemaphoreSlim semaphore)
    {
        Console.WriteLine("This seed is expected to last for 1000 seconds..");
        Console.WriteLine("Start: {0}", DateTimeOffset.Now);

        const string url = "http://localhost:5041/api/Incident";

        for (var i = 1; i <= 1000; i++)
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
            address = GenerateRandomAddress(),
            severity = EnumHelper.GetRandomEnumValue<SeverityEnum>(),
            crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>(),
            motive = EnumHelper.GetRandomEnumValue<MotiveEnum>(),
            policeDistrict = EnumHelper.GetRandomEnumValue<Barangay>(),
            otherMotive = "xxxxx",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp()
        };

        

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

    private static string GenerateRandomAddress() => AddressGenerator.GenerateRandomAddress();

    private static string GenerateRandomTimestamp()
    {
        var start = DateTime.UtcNow.AddYears(-1);
        var range = (DateTime.UtcNow - start).Days;
        var randomDate = start.AddDays(new Random().Next(range)).AddHours(new Random().Next(0, 24)).AddMinutes(new Random().Next(0, 60)).AddSeconds(new Random().Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    
}