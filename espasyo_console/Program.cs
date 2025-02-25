using System.Text;
using System.Text.Json;
using espasyo.Domain.Enums;
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
        var incident = new Request()
        {
            caseId = $"CASE-{i:D4}",
            address = GenerateRandomAddress(),
            severity = EnumHelper.GetRandomEnumValue<SeverityEnum>(),
            crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>(),
            motive = EnumHelper.GetRandomEnumValue<MotiveEnum>(),
            policeDistrict = EnumHelper.GetRandomEnumValue<MuntinlupaPoliceDistrictEnum>(),
            otherMotive = "string",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp()
        };

        

        var json = JsonSerializer.Serialize(incident);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Wait for the semaphore before sending the request
        await Semaphore.WaitAsync();
        try
        {
            var response = await Client.PostAsync(url, content);

            WriteLine(response.IsSuccessStatusCode
                ? $"Successfully created incident {incident.caseId}"
                : $"Failed to create incident {incident.caseId}: {response.StatusCode}");
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static string GenerateRandomAddress() => AddressGenerator.GenerateRandomAddress();

    private static string GenerateRandomTimestamp()
    {
        var start = DateTime.UtcNow.AddYears(-1);
        var range = (DateTime.UtcNow - start).Days;
        var randomDate = start.AddDays(Random.Next(range)).AddHours(Random.Next(0, 24)).AddMinutes(Random.Next(0, 60)).AddSeconds(Random.Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    
    
}

public record Request
{
    public string caseId { get; set; }
    public string address { get; set; }
    public SeverityEnum severity { get; set; }
    public CrimeTypeEnum crimeType { get; set; }
    public MotiveEnum motive { get; set; }
    public MuntinlupaPoliceDistrictEnum policeDistrict { get; set; }
    public string otherMotive { get; set; }
    public WeatherConditionEnum weather { get; set; }
    public string timeStamp { get; set; }
}