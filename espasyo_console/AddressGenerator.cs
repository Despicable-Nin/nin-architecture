using espasyo.Domain.Enums;

namespace espasyo_console;

public static class AddressGenerator
{
    private static readonly Random Random = new();

    // Dictionary mapping barangays to their suburbs/streets
    private static readonly Dictionary<MuntinlupaPoliceDistrictEnum, string[]> BarangaySuburbs = new()
    {
        { MuntinlupaPoliceDistrictEnum.Alabang, new[] { "Montillano St.", "Spectrum Midway", "Commerce Ave.", "Filinvest Ave.", "Madrigal Ave.", "Corporate Ave.", "Acacia Ave.", "Alabang-Zapote Rd.", "Westgate Center", "Northgate Cyberzone" } },
        { MuntinlupaPoliceDistrictEnum.Ayala_Alabang, new[] { "Acacia St.", "Country Club Dr.", "Molave St.", "Palm Ave.", "Mariposa Loop", "Narra St.", "Tanguile St.", "Madrigal Ave.", "Camachile St.", "Lauan St." } },
        { MuntinlupaPoliceDistrictEnum.Buli, new[] { "East Service Rd.", "West Service Rd.", "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Buli Road", "Bagong Lipunan St.", "Silangan St." } },
        { MuntinlupaPoliceDistrictEnum.Bayanan, new[] { "Bayanan Main Rd.", "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Lakeview Homes", "Baywalk Rd.", "Muntinlupa General Hospital Area", "Sto. Niño St." } },
        { MuntinlupaPoliceDistrictEnum.Cupang, new[] { "San Guillermo Ave.", "Sto. Niño St.", "Cupang Main Rd.", "Tawiran St.", "South Greenheights", "Mangga St.", "San Jose St.", "Madrigal Ave. Extension", "Poblacion Extension", "Lanzones St." } },
        { MuntinlupaPoliceDistrictEnum.Poblacion, new[] { "Tunasan Rd.", "New Bilibid Prison Rd.", "Gov. San Luis St.", "Camia St.", "Cuyab St.", "Dulong Bayan Rd.", "Poblacion Main Rd.", "Balimbing St.", "Evergreen St.", "Gen. A. Luna St." } },
        { MuntinlupaPoliceDistrictEnum.Putatan, new[] { "Putatan Main Rd.", "San Guillermo Ave.", "Soldiers Hills", "M.L. Quezon St.", "Bayfair St.", "Camella St.", "Sta. Teresita St.", "San Pedro St.", "Sitio Pagkakaisa", "Francisco St." } },
        { MuntinlupaPoliceDistrictEnum.Tunasan, new[] { "Tunasan Main Rd.", "Susana Heights Rd.", "Daang Hari Rd.", "Magsaysay St.", "Langgam St.", "St. Benedict St.", "Kamagong St.", "Cedarwood St.", "Macapagal Ave.", "South Park Ave." } }
    };

    public static string GenerateRandomAddress()
    {
        // Select a random barangay
        var barangay = new Random().Next(0, BarangaySuburbs.Count -1);
        var barangayEnum = (MuntinlupaPoliceDistrictEnum)barangay;
        
        // Select a random suburb/street from the chosen barangay
        var suburb = GetRandomValue(BarangaySuburbs[barangayEnum]);

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