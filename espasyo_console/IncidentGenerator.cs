using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class IncidentGenerator
{
    public static async Task<bool> ContinueToTargetCount(HttpClient httpClient, SemaphoreSlim semaphore, int targetCount)
    {
        Console.WriteLine($"🎯 Continuing incident seeding to reach {targetCount} total...");
        
        try
        {
            // Check current incident count
            var currentCount = await GetCurrentIncidentCount(httpClient);
            if (currentCount < 0)
            {
                Console.WriteLine("❌ Failed to check current incident count.");
                return false;
            }
            
            if (currentCount >= targetCount)
            {
                Console.WriteLine($"✅ Already have {currentCount} incidents (target: {targetCount}). Skipping.");
                return true;
            }
            
            var remaining = targetCount - currentCount;
            Console.WriteLine($"🌱 Need to create {remaining} more incidents (current: {currentCount}, target: {targetCount})");
            
            return await SeedIncidents(httpClient, semaphore, remaining, currentCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in incident continuation: {ex.Message}");
            return false;
        }
    }
    
    private static async Task<int> GetCurrentIncidentCount(HttpClient httpClient)
    {
        try
        {
            var response = await httpClient.GetAsync("http://localhost:5041/api/Incident");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("totalCount", out var totalCountProp))
                {
                    return totalCountProp.GetInt32();
                }
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to check incident count: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error getting incident count: {ex.Message}");
        }
        
        return -1; // Indicate error
    }
    
    private static async Task<bool> SeedIncidents(HttpClient httpClient, SemaphoreSlim semaphore, int count, int startIndex)
    {
        Console.WriteLine($"🌱 Seeding {count} incidents starting from index {startIndex + 1}...");
        Console.WriteLine($"⏱️ Start: {DateTimeOffset.Now}");
        
        const string url = "http://localhost:5041/api/Incident";
        var successCount = 0;
        
        for (var i = 0; i < count; i++)
        {
            var caseNumber = startIndex + i + 1;
            var timestampSuffix = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var caseId = $"CASE-{caseNumber:D4}-{timestampSuffix}";
            
            var success = await SendIncidentRequestWithId(url, caseId, httpClient, semaphore);
            if (!success)
            {
                Console.WriteLine($"❌ Stopping seeding at incident {i + 1} due to error.");
                Console.WriteLine($"📊 Final count: {successCount} incidents successfully created");
                return false;
            }
            
            successCount++;
            
            // Show progress every 10 incidents
            if ((i + 1) % 10 == 0)
            {
                Console.WriteLine($"📊 Progress: {successCount}/{count} new incidents created");
            }
            
            await Task.Delay(500); // Reduced delay for faster seeding
        }
        
        Console.WriteLine($"⏱️ End: {DateTimeOffset.Now}");
        Console.WriteLine($"✅ Successfully created {successCount} new incidents");
        return true;
    }
    
    private static async Task<bool> SendIncidentRequestWithId(string url, string caseId, HttpClient client, SemaphoreSlim semaphore)
    {
        // First, get a random precinct ID from the API
        var precinctId = await GetRandomPrecinctId(client);
        if (precinctId == null)
        {
            Console.WriteLine($"❌ Failed to get precinct for incident {caseId}. Stopping seeding.");
            return false;
        }
        
        var incident = new Request()
        {
            caseId = caseId,
            severity = EnumHelper.GetRandomEnumValue<SeverityEnum>(),
            crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>(),
            motive = EnumHelper.GetRandomEnumValue<MotiveEnum>(),
            precinct = (Barangay)new Random().Next(0, 7), // Keep for address generation
            precinctId = precinctId, // Add new PrecinctId field
            otherMotive = "xxxxx",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp()
        };
        
        incident.address = GenerateRandomAddress(incident.precinct);
        
        var json = JsonSerializer.Serialize(incident);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Wait for the semaphore before sending the request
        await semaphore.WaitAsync();
        try
        {
            var response = await client.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✓ Successfully created incident {incident.caseId}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Failed to create incident {incident.caseId}: {response.StatusCode}");
                Console.WriteLine($"📄 Request JSON: {json}");
                Console.WriteLine($"📄 Error Response: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception creating incident {incident.caseId}: {ex.Message}");
            Console.WriteLine($"📄 Request JSON: {json}");
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }
    private static async Task<bool> Seed(HttpClient httpClient, SemaphoreSlim semaphore)
    {
        Console.WriteLine("🌱 Starting incident seeding (this may take a while)...");
        Console.WriteLine($"⏱️ Start: {DateTimeOffset.Now}");

        const string url = "http://localhost:5041/api/Incident";
        var successCount = 0;
        const int maxIncidents = 10; // Reduced for testing, change back to 5000 if needed

        for (var i = 1; i <= maxIncidents; i++)
        {
            var success = await SendIncidentRequest(url, i, httpClient, semaphore);
            if (!success)
            {
                Console.WriteLine($"❌ Stopping seeding at incident {i} due to error.");
                Console.WriteLine($"📊 Final count: {successCount} incidents successfully created");
                return false; // Return false because we failed
            }
            
            successCount++;
            
            // Show progress every 5 incidents
            if (i % 5 == 0)
            {
                Console.WriteLine($"📊 Progress: {successCount}/{maxIncidents} incidents created");
            }
            
            await Task.Delay(1000); // Delay of 1 second between requests
        }

        Console.WriteLine($"⏱️ End: {DateTimeOffset.Now}");
        Console.WriteLine($"✅ Successfully created all {successCount} incidents");
        return true;
    }

    private static async Task<bool> SendIncidentRequest(string url, int i, HttpClient client, SemaphoreSlim semaphore)
    {
        // First, get a random precinct ID from the API
        var precinctId = await GetRandomPrecinctId(client);
        if (precinctId == null)
        {
            Console.WriteLine($"❌ Failed to get precinct for incident {i}. Stopping seeding.");
            return false;
        }

        var incident = new Request()
        {
            caseId = $"CASE-{i:D4}",
            severity = EnumHelper.GetRandomEnumValue<SeverityEnum>(),
            crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>(),
            motive = EnumHelper.GetRandomEnumValue<MotiveEnum>(),
            precinct = (Barangay)new Random().Next(0,7), // Keep for address generation
            precinctId = precinctId, // Add new PrecinctId field
            otherMotive = "xxxxx",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp()
        };

        incident.address = GenerateRandomAddress(incident.precinct);

        var json = JsonSerializer.Serialize(incident);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Wait for the semaphore before sending the request
        await semaphore.WaitAsync();
        try
        {
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✓ Successfully created incident {incident.caseId}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Failed to create incident {incident.caseId}: {response.StatusCode}");
                Console.WriteLine($"📄 Request JSON: {json}");
                Console.WriteLine($"📄 Error Response: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception creating incident {incident.caseId}: {ex.Message}");
            Console.WriteLine($"📄 Request JSON: {json}");
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string GenerateRandomAddress(Barangay barangay) => AddressGenerator.GenerateRandomAddress(barangay);
    
    private static async Task<string?> GetRandomPrecinctId(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("http://localhost:5041/api/manpower/precincts");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var precincts = JsonSerializer.Deserialize<PrecinctInfo[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (precincts != null && precincts.Length > 0)
                {
                    var randomIndex = new Random().Next(0, precincts.Length);
                    return precincts[randomIndex].Id;
                }
            }
            else
            {
                Console.WriteLine($"Failed to fetch precincts: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception fetching precincts: {ex.Message}");
        }
        
        return null;
    }
    
    private record PrecinctInfo(string Id, string Name, string Code);

    private static string GenerateRandomTimestamp()
    {
        var start = DateTime.UtcNow.AddYears(-5);
        var range = (DateTime.UtcNow - start).Days;
        var randomDate = start.AddDays(new Random().Next(range)).AddHours(new Random().Next(0, 24)).AddMinutes(new Random().Next(0, 60)).AddSeconds(new Random().Next(0, 60));
        return randomDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    public static async Task<bool> SeedIfNoIncidents(HttpClient httpClient, SemaphoreSlim semaphore, bool isNonInteractive = false)
    {
        Console.WriteLine("🔍 Checking for existing incidents...");
        const string url = "http://localhost:5041/api/Incident";

        try
        {
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                // Parse content as dynamic JSON object
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Try to get totalCount property
                int totalCount = 0;
                if (root.TryGetProperty("totalCount", out var totalCountProp))
                {
                    totalCount = totalCountProp.GetInt32();
                }

                if (totalCount > 0)
                {
                    Console.WriteLine($"📋 Found {totalCount} existing incidents. Proceeding with seeding...");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to check incidents: {response.StatusCode}. Proceeding with seeding.");
            }

            var success = await Seed(httpClient, semaphore);
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception checking incidents: {ex.Message}");
            return false;
        }
    }
    
}