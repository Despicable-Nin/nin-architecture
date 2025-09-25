using System.Text;
using System.Text.Json;

namespace espasyo_console;

public static class PrecinctSeeder
{
    public static async Task<bool> SeedMissingPrecincts(HttpClient httpClient)
    {
        Console.WriteLine("🔍 Checking existing precincts...");
        
        // Define expected precincts
        var expectedPrecincts = new[]
        {
            new { Name = "Alabang", Code = "ALB" },
            new { Name = "Ayala Alabang", Code = "AAL" },
            new { Name = "Sucat", Code = "SUC" },
            new { Name = "Poblacion", Code = "POB" },
            new { Name = "Putatan", Code = "PUT" },
            new { Name = "Tunasan", Code = "TUN" },
            new { Name = "Cupang", Code = "CUP" },
            new { Name = "Bayanan", Code = "BAY" },
            new { Name = "Buli", Code = "BUL" }
        };
        
        // Check which precincts already exist
        var existingPrecincts = await GetExistingPrecincts(httpClient);
        var existingCodes = existingPrecincts?.Select(p => p.Code).ToHashSet() ?? new HashSet<string>();
        
        var missingPrecincts = expectedPrecincts.Where(p => !existingCodes.Contains(p.Code)).ToArray();
        
        if (missingPrecincts.Length == 0)
        {
            Console.WriteLine($"✅ All {expectedPrecincts.Length} precincts already exist. Skipping.");
            return true;
        }
        
        Console.WriteLine($"🌱 Seeding {missingPrecincts.Length} missing precincts:");
        foreach (var precinct in missingPrecincts)
        {
            Console.WriteLine($"  - {precinct.Name} ({precinct.Code})");
        }

        // Define the detailed precinct data for the missing ones
        var allPrecinctData = new Dictionary<string, object>
        {
            ["ALB"] = new { Name = "Alabang", Code = "ALB", Population = 54000, AreaKm2 = 23.5m, Description = "Commercial and business district" },
            ["AAL"] = new { Name = "Ayala Alabang", Code = "AAL", Population = 25000, AreaKm2 = 8.2m, Description = "High-income residential area" },
            ["SUC"] = new { Name = "Sucat", Code = "SUC", Population = 42000, AreaKm2 = 15.7m, Description = "Mixed residential and commercial area" },
            ["POB"] = new { Name = "Poblacion", Code = "POB", Population = 18000, AreaKm2 = 5.3m, Description = "City center and administrative area" },
            ["PUT"] = new { Name = "Putatan", Code = "PUT", Population = 35000, AreaKm2 = 12.8m, Description = "Residential area with moderate density" },
            ["TUN"] = new { Name = "Tunasan", Code = "TUN", Population = 28000, AreaKm2 = 10.4m, Description = "Residential with some commercial areas" },
            ["CUP"] = new { Name = "Cupang", Code = "CUP", Population = 22000, AreaKm2 = 8.9m, Description = "Smaller residential area" },
            ["BAY"] = new { Name = "Bayanan", Code = "BAY", Population = 31000, AreaKm2 = 11.6m, Description = "Residential area" },
            ["BUL"] = new { Name = "Buli", Code = "BUL", Population = 26000, AreaKm2 = 9.8m, Description = "Residential area" }
        };
        
        // Only include precincts that are missing
        var precincts = missingPrecincts.Select(p => allPrecinctData[p.Code]).ToArray();

        var request = new
        {
            Precincts = precincts
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync("http://localhost:5041/api/manpower/precincts/upsert", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Precinct seeding response: {responseContent}");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Failed to seed precincts: {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception seeding precincts: {ex.Message}");
            return false;
        }
    }
    
    private static async Task<PrecinctInfo[]?> GetExistingPrecincts(HttpClient httpClient)
    {
        try
        {
            var response = await httpClient.GetAsync("http://localhost:5041/api/manpower/precincts");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PrecinctInfo[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error checking existing precincts: {ex.Message}");
        }
        return null;
    }
    
    private static async Task<bool> DeleteAllPrecincts(HttpClient httpClient)
    {
        try
        {
            // Note: This assumes there's a delete endpoint. 
            // For now, we'll just return true since upsert will overwrite
            Console.WriteLine("ℹ️ Using upsert to overwrite existing precincts.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting precincts: {ex.Message}");
            return false;
        }
    }
    
    private record PrecinctInfo(string Id, string Name, string Code);
}