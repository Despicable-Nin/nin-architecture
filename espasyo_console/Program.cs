using System.Text;
using System.Text.Json;

namespace espasyo_console;

public class Program
{
    static readonly HttpClient client = new HttpClient();
    static readonly Random random = new Random();

    static async Task Main(string[] args)
    {
        string url = "http://localhost:5041/api/Incident";

        for (int i = 0; i < 1000; i++)
        {
            var incident = new
            {
                caseId = $"CASE-{i:D4}",
                address = GenerateRandomAddress(),
                severity = random.Next(1, 4),
                crimeType = random.Next(1, 4),
                motive = random.Next(1, 4),
                policeDistrict = random.Next(1, 4),
                otherMotive = "string",
                weather = random.Next(1, 4),
                timeStamp = GenerateRandomTimestamp()
            };

            string json = JsonSerializer.Serialize(incident);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully created incident {incident.caseId}");
            }
            else
            {
                Console.WriteLine($"Failed to create incident {incident.caseId}: {response.StatusCode}");
            }
        }
    }

    static string GenerateRandomAddress()
    {
        string[] streets = { "Main St", "Rizal Ave", "Magsaysay Blvd", "National Road", "Alabang-Zapote Road" };
        string[] barangays = { "Alabang", "Bayanan", "Putatan", "Poblacion", "Tunasan" };
        return $"{random.Next(1, 1000)} {streets[random.Next(streets.Length)]}, {barangays[random.Next(barangays.Length)]}, Muntinlupa City, Philippines";
    }

    static string GenerateRandomTimestamp()
    {
        DateTime start = DateTime.UtcNow.AddYears(-1);
        int range = (DateTime.UtcNow - start).Days;
        DateTime randomDate = start.AddDays(random.Next(range)).AddHours(random.Next(0, 24)).AddMinutes(random.Next(0, 60)).AddSeconds(random.Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}