using System.Text.Json;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class AddressGenerator
{
    private static readonly Random Random = new();

    // Dictionary mapping barangays to their suburbs/streets
    private static readonly Dictionary<Barangay, string[]> BarangaySuburbs;

    private static readonly string[] AlabangStreets;
    private static readonly string[] AyalaAlabangStreets;
    private static readonly string[] BayananStreets;
    private static readonly string[] BuliStreets;
    private static readonly string[] CupangStreets;
    private static readonly string[] PoblacionStreets;
    private static readonly string[] PutatanStreets;
    private static readonly string[] TunasanStreets;
    private static readonly string[] SucatStreets;

    static AddressGenerator()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory).Parent.Parent.FullName.Replace("bin", "");
        var jsonFilesDirectory = Path.Combine(projectDirectory, "JsonFiles");
        var enums = (Barangay[])Enum.GetValues(typeof(Barangay));

        var streets = new List<StreetDto>();


        foreach (var e in enums)
        {
            Console.WriteLine($"Generating streets from {e}");
            var fileName = $"{e}.json";
            var filePath = Path.Combine(jsonFilesDirectory, fileName);
            var jsonContent = File.ReadAllText(filePath);

            var obj = JsonSerializer.Deserialize<StreetDto?[]>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (e == Barangay.Alabang)
            {
                AlabangStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Ayala_Alabang)
            {
                AyalaAlabangStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Buli)
            {
                BuliStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Bayanan)
            {
                BayananStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Cupang)
            {
                CupangStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Poblacion)
            {
                PoblacionStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Putatan)
            {
                PutatanStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else if (e == Barangay.Tunasan)
            {
                TunasanStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
            else
            {
                SucatStreets = obj?.Select(x => x.Street).Distinct().ToArray();
            }
        }
        
        BarangaySuburbs= new Dictionary<Barangay, string[]>
        {
            { Barangay.Alabang, AlabangStreets},
            { Barangay.Ayala_Alabang, AyalaAlabangStreets },
            { Barangay.Buli, BuliStreets },
            { Barangay.Bayanan, BayananStreets },
            { Barangay.Cupang, CupangStreets },
            { Barangay.Poblacion, PoblacionStreets },
            { Barangay.Putatan, PutatanStreets },
            { Barangay.Tunasan, TunasanStreets }
        };
    }

     public static string GenerateRandomAddress()
    {
        // Select a random barangay
        var barangay = new Random().Next(0, BarangaySuburbs.Count -1);
        var barangayEnum = (Barangay)barangay;
        
        // Select a random suburb/street from the chosen barangay
        var suburb = BarangaySuburbs[barangayEnum][new Random().Next(0, BarangaySuburbs[barangayEnum].Length - 1)];

        // Generate a random house number
        var houseNumber = Random.Next(1, 999);

        // Construct the address
        return $"{houseNumber} {suburb}, {barangayEnum.ToString()}, Muntinlupa City, Philippines";
    }

    // Helper method to get a random value from an array
    private static string GetRandomValue(string[] array)
    {
        return array[Random.Next(array.Length)];
    }
}