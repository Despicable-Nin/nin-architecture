using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class IncidentGenerator
{
    private static readonly Random Rng = new();
    private static readonly DateTime MaxSeedDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime MinSeedDate = MaxSeedDate.AddYears(-5);

    private static readonly Dictionary<CrimeTypeEnum, List<MotiveEnum>> ValidMotives = new()
    {
        [CrimeTypeEnum.Arson] = new() { MotiveEnum.Anger, MotiveEnum.Revenge, MotiveEnum.Terrorism, MotiveEnum.Unknown, MotiveEnum.Other },
        [CrimeTypeEnum.Assault] = new() { MotiveEnum.Anger, MotiveEnum.Jealousy, MotiveEnum.Revenge, MotiveEnum.Unknown, MotiveEnum.Other },
        [CrimeTypeEnum.Burglary] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Corruption] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Political, MotiveEnum.Unknown },
        [CrimeTypeEnum.Counterfeiting] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.CyberCrime] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Revenge, MotiveEnum.Terrorism, MotiveEnum.Unknown },
        [CrimeTypeEnum.DomesticViolence] = new() { MotiveEnum.Anger, MotiveEnum.Jealousy, MotiveEnum.Unknown, MotiveEnum.Other },
        [CrimeTypeEnum.DrugTrafficking] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Embezzlement] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Extortion] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Fraud] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.HumanTrafficking] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Homicide] = new() { MotiveEnum.Anger, MotiveEnum.Jealousy, MotiveEnum.Revenge, MotiveEnum.Unknown },
        [CrimeTypeEnum.IllegalPossessionOfFirearms] = new() { MotiveEnum.PersonalGain, MotiveEnum.Terrorism, MotiveEnum.Unknown },
        [CrimeTypeEnum.Kidnapping] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Revenge, MotiveEnum.Political, MotiveEnum.Terrorism, MotiveEnum.Unknown },
        [CrimeTypeEnum.Murder] = new() { MotiveEnum.Anger, MotiveEnum.Jealousy, MotiveEnum.Revenge, MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Rape] = new() { MotiveEnum.Anger, MotiveEnum.Revenge, MotiveEnum.Unknown, MotiveEnum.Other },
        [CrimeTypeEnum.Robbery] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Theft] = new() { MotiveEnum.Greed, MotiveEnum.PersonalGain, MotiveEnum.Unknown },
        [CrimeTypeEnum.Vandalism] = new() { MotiveEnum.Anger, MotiveEnum.Revenge, MotiveEnum.Terrorism, MotiveEnum.Unknown, MotiveEnum.Other },
    };

    private static readonly Dictionary<CrimeTypeEnum, SeverityEnum> MinimumSeverity = new()
    {
        [CrimeTypeEnum.Arson] = SeverityEnum.Low,
        [CrimeTypeEnum.Assault] = SeverityEnum.Medium,
        [CrimeTypeEnum.Burglary] = SeverityEnum.Low,
        [CrimeTypeEnum.Corruption] = SeverityEnum.Medium,
        [CrimeTypeEnum.Counterfeiting] = SeverityEnum.Low,
        [CrimeTypeEnum.CyberCrime] = SeverityEnum.Low,
        [CrimeTypeEnum.DomesticViolence] = SeverityEnum.Medium,
        [CrimeTypeEnum.DrugTrafficking] = SeverityEnum.High,
        [CrimeTypeEnum.Embezzlement] = SeverityEnum.Medium,
        [CrimeTypeEnum.Extortion] = SeverityEnum.Medium,
        [CrimeTypeEnum.Fraud] = SeverityEnum.Low,
        [CrimeTypeEnum.HumanTrafficking] = SeverityEnum.High,
        [CrimeTypeEnum.Homicide] = SeverityEnum.High,
        [CrimeTypeEnum.IllegalPossessionOfFirearms] = SeverityEnum.Medium,
        [CrimeTypeEnum.Kidnapping] = SeverityEnum.High,
        [CrimeTypeEnum.Murder] = SeverityEnum.High,
        [CrimeTypeEnum.Rape] = SeverityEnum.High,
        [CrimeTypeEnum.Robbery] = SeverityEnum.Medium,
        [CrimeTypeEnum.Theft] = SeverityEnum.Low,
        [CrimeTypeEnum.Vandalism] = SeverityEnum.Low,
    };

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
        // Fetch the real precinct data (id + barangay enum) to keep address geographically aligned
        var precinctData = await GetRandomPrecinctWithBarangay(client);
        if (precinctData == null)
        {
            Console.WriteLine($"❌ Failed to get precinct for incident {caseId}. Stopping seeding.");
            return false;
        }
        
        var incident = BuildRandomIncident(caseId, precinctData.Value);
        
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
        // Fetch the real precinct data (id + barangay enum) to keep address geographically aligned
        var precinctData = await GetRandomPrecinctWithBarangay(client);
        if (precinctData == null)
        {
            Console.WriteLine($"❌ Failed to get precinct for incident {i}. Stopping seeding.");
            return false;
        }

        var incident = BuildRandomIncident($"CASE-{i:D4}", precinctData.Value);

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

    /// <summary>
    /// Fetches a random precinct from the API and maps its Code to the correct Barangay enum
    /// so that address generation is geographically consistent with the chosen precinct.
    /// </summary>
    private static async Task<(string Id, Barangay Barangay)?> GetRandomPrecinctWithBarangay(HttpClient client)
    {
        // Map precinct code → Barangay enum value (must match SqliteApplicationDbContext seed)
        var codeToBarangay = new Dictionary<string, Barangay>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALB"] = Barangay.Alabang,
            ["AAL"] = Barangay.Ayala_Alabang,
            ["SUC"] = Barangay.Sucat,
            ["POB"] = Barangay.Poblacion,
            ["PUT"] = Barangay.Putatan,
            ["TUN"] = Barangay.Tunasan,
            ["CUP"] = Barangay.Cupang,
            ["BAY"] = Barangay.Bayanan,
            ["BUL"] = Barangay.Buli,
        };

        try
        {
            var response = await client.GetAsync("http://localhost:5041/api/manpower/precincts");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var precincts = JsonSerializer.Deserialize<PrecinctInfoFull[]>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (precincts != null && precincts.Length > 0)
                {
                    var chosen = precincts[new Random().Next(0, precincts.Length)];
                    if (codeToBarangay.TryGetValue(chosen.Code, out var barangay))
                        return (chosen.Id, barangay);

                    // Fallback: if code not in map use first known barangay
                    Console.WriteLine($"⚠️ Precinct code '{chosen.Code}' not in barangay map, defaulting to Alabang");
                    return (chosen.Id, Barangay.Alabang);
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

    // Keep the old ID-only version for backward compatibility but it is no longer called
    private static async Task<string?> GetRandomPrecinctId(HttpClient client)
    {
        var result = await GetRandomPrecinctWithBarangay(client);
        return result?.Id;
    }

    private record PrecinctInfo(string Id, string Name, string Code);
    private record PrecinctInfoFull(string Id, string Name, string Code);

    private static Request BuildRandomIncident(string caseId, (string Id, Barangay Barangay) precinctData)
    {
        var crimeType = EnumHelper.GetRandomEnumValue<CrimeTypeEnum>();
        var (lat, lng) = CoordinateGenerator.GetRandomPoint(precinctData.Barangay);
        return new Request()
        {
            caseId = caseId,
            severity = GetRandomSeverity(crimeType),
            crimeType = crimeType,
            motive = GetRandomMotive(crimeType),
            precinct = precinctData.Barangay,
            precinctId = precinctData.Id,
            otherMotive = "xxxxx",
            weather = EnumHelper.GetRandomEnumValue<WeatherConditionEnum>(),
            timeStamp = GenerateRandomTimestamp(),
            latitude = lat,
            longitude = lng
        };
    }

    private static SeverityEnum GetRandomSeverity(CrimeTypeEnum crimeType)
    {
        var min = (int)MinimumSeverity.GetValueOrDefault(crimeType, SeverityEnum.Low);
        var max = (int)SeverityEnum.High;
        return (SeverityEnum)Rng.Next(min, max + 1);
    }

    private static MotiveEnum GetRandomMotive(CrimeTypeEnum crimeType)
    {
        var motives = ValidMotives.GetValueOrDefault(crimeType, new() { MotiveEnum.Unknown });
        return motives[Rng.Next(motives.Count)];
    }

    private static string GenerateRandomTimestamp()
    {
        var range = (MaxSeedDate - MinSeedDate).Days;
        var randomDate = MinSeedDate.AddDays(Rng.Next(range + 1)).AddHours(Rng.Next(0, 24)).AddMinutes(Rng.Next(0, 60)).AddSeconds(Rng.Next(0, 60));
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