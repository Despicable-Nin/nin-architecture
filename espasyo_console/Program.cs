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

        Console.WriteLine("1. Seed Streets");
        Console.WriteLine("2. Seed Incidents");
        Console.Write("Choose an option: ");
        var choice = Console.ReadLine();
        if (choice == "1")
        {
            await StreetsGenerator.Seed(Client);
        }
        else if (choice == "2")
        {
            await IncidentGenerator.Seed(Client,Semaphore);
        }
        else
        {
            Console.WriteLine("Invalid choice, please try again.");
        }

        WriteLine("Press any key to exit...");
    }
}

public record Request
{
    public string caseId { get; set; }
    public string address { get; set; }
    public SeverityEnum severity { get; set; }
    public CrimeTypeEnum crimeType { get; set; }
    public MotiveEnum motive { get; set; }
    public Barangay policeDistrict { get; set; }
    public string otherMotive { get; set; }
    public WeatherConditionEnum weather { get; set; }
    public string timeStamp { get; set; }
}