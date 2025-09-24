using System.Text;
using System.Text.Json;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class ManpowerSeeder
{
    public static async Task SeedCurrentYearManpower(HttpClient client)
    {
        Console.WriteLine("Seeding manpower allocations for current year...");

        var currentYear = DateTime.Now.Year;

        // Realistic manpower allocations for all Muntinlupa barangays
        // Based on population density, crime rates, and area coverage
        var manpowerAllocations = new[]
        {
            // Alabang - commercial/business district, higher crime potential
            new { Precinct = Barangay.Alabang, Year = currentYear, AllocatedCount = 35, MildThreshold = 25, ModerateThreshold = 40, CriticalThreshold = 60 },
            
            // Ayala Alabang - high-income residential, lower crime but needs presence
            new { Precinct = Barangay.Ayala_Alabang, Year = currentYear, AllocatedCount = 28, MildThreshold = 20, ModerateThreshold = 32, CriticalThreshold = 48 },
            
            // Sucat - mixed residential/commercial, moderate crime
            new { Precinct = Barangay.Sucat, Year = currentYear, AllocatedCount = 30, MildThreshold = 22, ModerateThreshold = 35, CriticalThreshold = 52 },
            
            // Poblacion - city center, administrative area
            new { Precinct = Barangay.Poblacion, Year = currentYear, AllocatedCount = 32, MildThreshold = 24, ModerateThreshold = 38, CriticalThreshold = 55 },
            
            // Putatan - residential area, moderate density
            new { Precinct = Barangay.Putatan, Year = currentYear, AllocatedCount = 26, MildThreshold = 18, ModerateThreshold = 30, CriticalThreshold = 45 },
            
            // Tunasan - residential, some commercial
            new { Precinct = Barangay.Tunasan, Year = currentYear, AllocatedCount = 24, MildThreshold = 17, ModerateThreshold = 28, CriticalThreshold = 42 },
            
            // Cupang - smaller residential area
            new { Precinct = Barangay.Cupang, Year = currentYear, AllocatedCount = 20, MildThreshold = 14, ModerateThreshold = 24, CriticalThreshold = 36 },
            
            // Bayanan - residential area
            new { Precinct = Barangay.Bayanan, Year = currentYear, AllocatedCount = 22, MildThreshold = 16, ModerateThreshold = 26, CriticalThreshold = 38 },
            
            // Buli - residential area  
            new { Precinct = Barangay.Buli, Year = currentYear, AllocatedCount = 21, MildThreshold = 15, ModerateThreshold = 25, CriticalThreshold = 37 }
        };

        var successCount = 0;
        foreach (var allocation in manpowerAllocations)
        {
            if (await CreateManpowerAllocation(client, allocation.Precinct, allocation.Year, 
                allocation.AllocatedCount, allocation.MildThreshold, 
                allocation.ModerateThreshold, allocation.CriticalThreshold))
            {
                successCount++;
            }
            
            await Task.Delay(100); // Small delay between requests
        }

        Console.WriteLine($"Successfully seeded {successCount}/{manpowerAllocations.Length} manpower allocations for {currentYear}.");
    }

    private static async Task<bool> CreateManpowerAllocation(HttpClient client, Barangay precinct, int year, 
        int allocatedCount, int mildThreshold, int moderateThreshold, int criticalThreshold)
    {
        var manpower = new
        {
            Precinct = precinct,
            Year = year,
            AllocatedCount = allocatedCount,
            MildThreshold = mildThreshold,
            ModerateThreshold = moderateThreshold,
            CriticalThreshold = criticalThreshold
        };

        var json = JsonSerializer.Serialize(manpower);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("https://localhost:5041/api/manpower", content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✓ Created manpower allocation: {precinct} {year} ({allocatedCount} officers)");
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.WriteLine($"⚠ Manpower allocation already exists: {precinct} {year}");
                return false; // Don't count as success for new creation
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Failed to create manpower allocation for {precinct} {year}: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Exception creating manpower allocation for {precinct} {year}: {ex.Message}");
            return false;
        }
    }
}