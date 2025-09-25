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
        
        // Parse custom wait time if provided
        var waitTimeSeconds = 60; // default
        var waitTimeArg = args.FirstOrDefault(arg => arg.StartsWith("--wait="));
        if (waitTimeArg != null && int.TryParse(waitTimeArg.Substring(7), out var customWaitTime))
        {
            waitTimeSeconds = customWaitTime;
        }
        
        // Wait for API server to be available
        WriteLine("\n🔌 Waiting for API server to be available...");
        var serverReady = await WaitForApiServer(Client, waitTimeSeconds);
        if (!serverReady)
        {
            WriteLine("❌ API server is not available after waiting. Please start the API server first.");
            WriteLine();
            WriteLine("💡 To start the API server:");
            WriteLine("   dotnet run --project espasyo.WebAPI");
            WriteLine();
            WriteLine("💡 Console seeder usage:");
            WriteLine("   dotnet run --project espasyo_console              # Interactive mode, 60s wait");
            WriteLine("   dotnet run --project espasyo_console -- -n        # Non-interactive mode");
            WriteLine("   dotnet run --project espasyo_console -- --wait=30 # Custom wait time (30s)");
            return;
        }
        WriteLine("✅ API server is ready!");

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
    
    private static async Task<bool> WaitForApiServer(HttpClient client, int maxWaitTimeSeconds = 60)
    {
        const string healthCheckUrl = "http://localhost:5041/api/manpower/precincts";
        var startTime = DateTime.Now;
        var maxWaitTime = TimeSpan.FromSeconds(maxWaitTimeSeconds);
        
        WriteLine($"⏱️ Checking API server availability (max wait: {maxWaitTimeSeconds}s)...");
        
        while (DateTime.Now - startTime < maxWaitTime)
        {
            try
            {
                using var response = await client.GetAsync(healthCheckUrl);
                // We don't care about the response content, just that we can connect
                WriteLine($"🎯 API server responded with status: {response.StatusCode}");
                return true; // Server is responsive
            }
            catch (HttpRequestException)
            {
                // Server not ready yet, continue waiting
                Write(".");
                await Task.Delay(2000); // Wait 2 seconds before retrying
            }
            catch (TaskCanceledException)
            {
                // Timeout on request, continue waiting  
                Write(".");
                await Task.Delay(2000);
            }
        }
        
        WriteLine();
        WriteLine($"⏰ Timeout: API server did not become available within {maxWaitTimeSeconds} seconds.");
        return false;
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
