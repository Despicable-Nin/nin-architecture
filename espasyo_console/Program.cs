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
    
    // Removed AskUserConfirmation - using smart seeding logic instead

    private static async Task Main(string[] args)
    {
        WriteLine("🌱 Starting seeding process...");
        
        // Check if we should run in non-interactive mode
        var isNonInteractive = args.Contains("--non-interactive") || args.Contains("-n");

        try
        {
            // 1. Smart precinct seeding - only seed missing precincts
            WriteLine("\n📍 Smart Precinct Seeding...");
            var precinctSuccess = await PrecinctSeeder.SeedMissingPrecincts(Client);
            if (!precinctSuccess)
            {
                WriteLine("❌ Failed to seed precincts. Stopping.");
                return;
            }
            
            // Wait a moment for precincts to be created
            await Task.Delay(500);

            // 2. Streets - remove all and re-seed
            WriteLine("\n🛣️ Refreshing Streets (remove all + re-seed)...");
            var streetsSuccess = await StreetsGenerator.RefreshAllStreets(Client);
            if (!streetsSuccess)
            {
                WriteLine("❌ Failed to refresh streets. Stopping.");
                return;
            }
            
            // 3. Incidents - continue from where left off (target: 5000 total)
            WriteLine("\n🚨 Smart Incident Seeding (continue to 5000 total)...");
            var incidentsSuccess = await IncidentGenerator.ContinueToTargetCount(Client, Semaphore, 5000);
            if (!incidentsSuccess)
            {
                WriteLine("❌ Failed to seed incidents. Stopping.");
                return;
            }
            
            // 4. Manpower - seed missing allocations
            WriteLine("\n👮 Smart Manpower Seeding...");
            var manpowerSuccess = await ManpowerSeeder.SeedMissingManpower(Client);
            if (!manpowerSuccess)
            {
                WriteLine("❌ Failed to seed manpower. Stopping.");
                return;
            }

            WriteLine("\n✅ Smart seeding process completed successfully!");
        }
        catch (Exception ex)
        {
            WriteLine($"❌ Fatal error during seeding: {ex.Message}");
            WriteLine($"📄 Stack trace: {ex.StackTrace}");
            return;
        }
        
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
    public Barangay precinct { get; set; } // Keep for address generation
    public string precinctId { get; set; } // New field for API
    public string otherMotive { get; set; }
    public WeatherConditionEnum weather { get; set; }
    public string timeStamp { get; set; }
}
