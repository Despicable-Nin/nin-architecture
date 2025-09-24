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
        
        // Check if we should run in non-interactive mode
        var isNonInteractive = args.Contains("--non-interactive") || args.Contains("-n");

        // Automatically seed streets
        WriteLine("Seeding Streets...");
        await StreetsGenerator.Seed(Client);
        
        // Automatically seed incidents
        WriteLine("Seeding Incidents...");
        await IncidentGenerator.SeedIfNoIncidents(Client, Semaphore);
        
        // Automatically seed work force allocations
        WriteLine("Seeding Manpower Allocations...");
        await ManpowerSeeder.SeedCurrentYearManpower(Client);

        WriteLine("Seeding process complete.");
        
        // Handle exit behavior based on environment and arguments
        if (isNonInteractive)
        {
            WriteLine("Running in non-interactive mode. Exiting automatically.");
            return;
        }
        
        // Try to wait for user input, but handle cases where it's not available
        try
        {
            if (!IsInputRedirected && Environment.UserInteractive)
            {
                WriteLine("Press any key to exit...");
                ReadKey();
            }
            else
            {
                WriteLine("Console input not available. Exiting automatically.");
            }
        }
        catch (InvalidOperationException)
        {
            WriteLine("Console input not available. Exiting automatically.");
        }
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