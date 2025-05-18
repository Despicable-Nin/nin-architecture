using System.Text;
using System.Text.Json;
using espasyo.Domain.Enums;
using static System.Console;

namespace espasyo_console;

public class Program
{
    private static readonly HttpClient Client = new();
    private static readonly Random Random = new();
    private static readonly SemaphoreSlim Semaphore = new(1, 1); // Allow only 1 concurrent request

    private static async Task Main(string[] args)
    {
        WriteLine("Starting seeding process...");

        // Automatically seed streets
        WriteLine("Seeding Streets...");
        await StreetsGenerator.Seed(Client);
        
        // Automatically seed incidents
        WriteLine("Seeding Incidents...");
        await IncidentGenerator.SeedIfNoIncidents(Client, Semaphore);

        WriteLine("Seeding process complete.");
        WriteLine("Press any key to exit...");
        ReadKey();
    }
}

public record Request
{
    public string caseId { get; set; }
    public string address { get; set; }
    public SeverityEnum severity { get; set; }
    public CrimeTypeEnum crimeType { get; set; }
    public MotiveEnum motive { get; set; }
    public Barangay precinct { get; set; }
    public string otherMotive { get; set; }
    public WeatherConditionEnum weather { get; set; }
    public string timeStamp { get; set; }
}