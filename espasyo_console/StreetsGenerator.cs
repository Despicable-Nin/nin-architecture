using System.Text;
using System.Text.Json;

namespace espasyo_console;

public record StreetDto
{
    public string? Street { get; init; }
    public string PrecinctId { get; set; } = string.Empty;
    public string PrecinctName { get; set; } = string.Empty;

    public void Update(string precinctId, string precinctName)
    {
        PrecinctId = precinctId;
        PrecinctName = precinctName;
    }
}

public record PrecinctDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public static class StreetsGenerator
{
    public static async Task<bool> RefreshAllStreets(HttpClient httpClient)
    {
        Console.WriteLine("🗑️ Refreshing all streets (delete + re-seed)...");
        
        try
        {
            // Delete existing streets (if any)
            await DeleteAllStreets(httpClient);
            
            // Seed new streets
            return await SeedAllStreets(httpClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in street refresh: {ex.Message}");
            return false;
        }
    }
    
    private static async Task DeleteAllStreets(HttpClient httpClient)
    {
        try
        {
            var response = await httpClient.DeleteAsync("http://localhost:5041/api/Street");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Successfully deleted existing streets.");
            }
            else
            {
                Console.WriteLine($"⚠️ Could not delete existing streets: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error deleting streets: {ex.Message}");
        }
    }
    
    private static async Task<bool> SeedAllStreets(HttpClient httpClient)
    {
        Console.WriteLine("🌱 Seeding new streets...");
        return await SeedStreetsInternal(httpClient);
    }

    public static async Task<bool> Seed(HttpClient httpClient, bool isNonInteractive = false)
    {
        // Legacy method for backward compatibility
        return await SeedStreetsInternal(httpClient);
    }
    
    private static async Task<bool> SeedStreetsInternal(HttpClient httpClient)

    {
        try
        {
            // First, get all precincts from the API
            var precincts = await GetPrecincts(httpClient);
            if (precincts == null || precincts.Length == 0)
            {
                Console.WriteLine("❌ No precincts found. Cannot seed streets.");
                return false;
            }

            var streets = new List<StreetDto>();

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectDirectory = Directory.GetParent(baseDirectory).Parent.Parent.FullName.Replace("bin", "");
            var jsonFilesDirectory = Path.Combine(projectDirectory, "JsonFiles");

            Console.WriteLine($"🌱 Generating streets for {precincts.Length} precincts...");
            foreach (var precinct in precincts)
            {
                Console.WriteLine($"  - Processing streets for {precinct.Name}");
                
                // Map old barangay names to precinct names for backward compatibility
                var fileName = GetFileNameForPrecinct(precinct.Name);
                if (fileName != null)
                {
                    var filePath = Path.Combine(jsonFilesDirectory, fileName);
                    
                    if (File.Exists(filePath))
                    {
                        var jsonContent = File.ReadAllText(filePath);
                        var obj = JsonSerializer.Deserialize<StreetDto?[]>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (obj != null && obj.Length > 0)
                        {
                            foreach (var s in obj)
                            {
                                if (s != null)
                                {
                                    s.Update(precinct.Id, precinct.Name);
                                    streets.Add(s);
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ No street data file found for {precinct.Name} (expected: {fileName})");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ No mapping found for precinct: {precinct.Name}");
                }
            }

            Console.WriteLine($"📊 Total streets to seed: {streets.Count}");
            const string url = "http://localhost:5041/api/Street";
            var success = await SendAddressRequest(url, streets, httpClient);
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in street seeding: {ex.Message}");
            return false;
        }
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
    
    private static string? GetFileNameForPrecinct(string precinctName)
    {
        // Map precinct names to JSON file names for backward compatibility
        // This assumes the JSON files are named after the old Barangay enum values
        return precinctName.ToLower() switch
        {
            "alabang" => "Alabang.json",
            "ayala alabang" => "Ayala_Alabang.json",
            "sucat" => "Sucat.json",
            "poblacion" => "Poblacion.json",
            "putatan" => "Putatan.json",
            "tunasan" => "Tunasan.json",
            "cupang" => "Cupang.json",
            "bayanan" => "Bayanan.json",
            "buli" => "Buli.json",
            _ => null
        };
    }

    private static async Task<bool> SendAddressRequest(string url, IEnumerable<StreetDto> streets, HttpClient client)
    {
        dynamic data = new
        {
            streets = streets
        };

        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Street creation successful: {response.StatusCode}");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Street creation failed: {response.StatusCode}");
                Console.WriteLine($"📄 Error details: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in street creation: {ex.Message}");
            return false;
        }
    }
}