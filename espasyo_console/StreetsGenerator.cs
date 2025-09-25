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
    public static async Task<bool> Seed(HttpClient httpClient)
    {
        var url = "http://localhost:5041/api/Street";
        // Check if data exists at the URL
        var getResponse = await httpClient.GetAsync(url);
        if (getResponse.IsSuccessStatusCode)
        {
            var content = await getResponse.Content.ReadAsStringAsync();
            // Check for empty JSON array or empty "streets" property
            bool isEmpty = false;
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        isEmpty = doc.RootElement.GetArrayLength() == 0;
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("streets", out var streetsProp))
                    {
                        isEmpty = streetsProp.ValueKind == JsonValueKind.Array && streetsProp.GetArrayLength() == 0;
                    }
                }
                catch
                {
                    // If parsing fails, treat as not empty to avoid seeding
                    isEmpty = false;
                }
            }
            else
            {
                isEmpty = true;
            }

            if (!isEmpty)
            {
                Console.WriteLine("Street data already exists. Skipping seed.");
                return false;
            }
        }

        // First, get all precincts from the API
        var precincts = await GetPrecincts(httpClient);
        if (precincts == null || precincts.Length == 0)
        {
            Console.WriteLine("No precincts found. Cannot seed streets.");
            return false;
        }

        var streets = new List<StreetDto>();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory).Parent.Parent.FullName.Replace("bin", "");
        var jsonFilesDirectory = Path.Combine(projectDirectory, "JsonFiles");

        foreach (var precinct in precincts)
        {
            Console.WriteLine($"Generating streets for {precinct.Name}");
            
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
            }
        }

        await SendAddressRequest(url, streets, httpClient);

        return true;
    }

    private static async Task<PrecinctDto[]?> GetPrecincts(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("http://localhost:5041/api/precinct");
            
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

    private static async Task SendAddressRequest(string url, IEnumerable<StreetDto> streets, HttpClient client)
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
            Console.WriteLine("Street creation is: " + response.StatusCode);
        }
        finally
        {

        }
    }
}