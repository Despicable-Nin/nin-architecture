using espasyo.Domain.Enums;
using System.Text;
using System.Text.Json;

namespace espasyo_console;
public record StreetDto
{
    public string? Street { get; init; }
    public Barangay Barangay { get; private set; }

    public void Update(Barangay barangay)
    {
        Barangay = barangay;
    }
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

        var enums = (Barangay[])Enum.GetValues(typeof(Barangay));
        var streets = new List<StreetDto>();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory).Parent.Parent.FullName.Replace("bin", "");
        var jsonFilesDirectory = Path.Combine(projectDirectory, "JsonFiles");

        foreach (var e in enums)
        {
            Console.WriteLine($"Generating streets from {e}");
            var fileName = $"{e}.json";
            var filePath = Path.Combine(jsonFilesDirectory, fileName);
            var jsonContent = File.ReadAllText(filePath);

            var obj = JsonSerializer.Deserialize<StreetDto?[]>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (obj != null && obj.Length > 0)
            {
                foreach (var s in obj)
                {
                    if (s != null)
                    {
                        s.Update(e);
                        streets.Add(s);
                    }
                }
            }
        }

        await SendAddressRequest(url, streets, httpClient);

        return true;
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