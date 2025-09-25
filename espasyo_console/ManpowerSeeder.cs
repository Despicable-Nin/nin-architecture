using System.Text;
using System.Text.Json;

namespace espasyo_console;

public static class ManpowerSeeder
{
    private static readonly Random Random = new();
    
    public static async Task<bool> SeedMissingManpower(HttpClient client)
    {
        Console.WriteLine("🔍 Checking existing manpower allocations...");
        
        try
        {
            // Get all precincts
            var precincts = await GetPrecincts(client);
            if (precincts == null || precincts.Length == 0)
            {
                Console.WriteLine("❌ No precincts found. Cannot seed manpower allocations.");
                return false;
            }

            // Get existing manpower allocations
            var existingManpower = await GetExistingManpower(client);
            var existingPrecinctIds = new HashSet<string>();
            
            if (existingManpower != null)
            {
                // Parse existing manpower to get precinct IDs
                foreach (var item in existingManpower)
                {
                    var jsonElement = (JsonElement)item;
                    if (jsonElement.TryGetProperty("precinctId", out var precinctIdProp))
                    {
                        existingPrecinctIds.Add(precinctIdProp.GetString() ?? "");
                    }
                }
            }

            // Find precincts without manpower allocations
            var precinctsMissingManpower = precincts.Where(p => !existingPrecinctIds.Contains(p.Id)).ToArray();
            
            if (precinctsMissingManpower.Length == 0)
            {
                Console.WriteLine($"✅ All {precincts.Length} precincts already have manpower allocations. Skipping.");
                return true;
            }

            Console.WriteLine($"🌱 Seeding manpower for {precinctsMissingManpower.Length} precincts missing allocations:");
            foreach (var precinct in precinctsMissingManpower)
            {
                Console.WriteLine($"  - {precinct.Name} ({precinct.Code})");
            }
            
            var successCount = 0;
            
            foreach (var precinct in precinctsMissingManpower)
            {
                // Generate random head count between 15-40 officers per precinct
                var headCount = Random.Next(15, 41);
                
                if (await CreateManpowerAllocation(client, precinct.Id, headCount))
                {
                    successCount++;
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create manpower allocation for {precinct.Name}. Continuing...");
                }
                
                await Task.Delay(100); // Small delay between requests
            }

            Console.WriteLine($"✅ Successfully seeded {successCount}/{precinctsMissingManpower.Length} missing manpower allocations.");
            return successCount > 0 || precinctsMissingManpower.Length == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in manpower seeding: {ex.Message}");
            return false;
        }
    }
    
    private static async Task<object[]?> GetExistingManpower(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("http://localhost:5041/api/manpower");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var manpowerList = JsonSerializer.Deserialize<object[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return manpowerList;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error checking existing manpower: {ex.Message}");
        }
        return null;
    }

    private static async Task<PrecinctDto[]?> GetPrecincts(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("http://localhost:5041/api/manpower/precincts");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PrecinctDto[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            else
            {
                Console.WriteLine($"Failed to fetch precincts: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception fetching precincts: {ex.Message}");
            return null;
        }
    }

    private static async Task<bool> CreateManpowerAllocation(HttpClient client, string precinctId, int headCount)
    {
        var manpower = new
        {
            PrecinctId = precinctId,
            HeadCount = headCount
        };

        var json = JsonSerializer.Serialize(manpower);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("http://localhost:5041/api/manpower", content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✓ Created manpower allocation: Precinct {precinctId} ({headCount} officers)");
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.WriteLine($"⚠ Manpower allocation already exists for precinct {precinctId}");
                return false; // Don't count as success for new creation
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Failed to create manpower allocation for precinct {precinctId}: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Exception creating manpower allocation for precinct {precinctId}: {ex.Message}");
            return false;
        }
    }
    
    public class PrecinctDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}