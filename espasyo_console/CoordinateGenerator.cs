using System.Text.Json;
using espasyo.Domain.Enums;

namespace espasyo_console;

public static class CoordinateGenerator
{
    private static readonly Random Rng = new();
    private static readonly Dictionary<Barangay, (double MinLat, double MaxLat, double MinLng, double MaxLng, List<(double Lng, double Lat)> Boundary)> Boundaries;

    static CoordinateGenerator()
    {
        var nameToBarangay = new Dictionary<string, Barangay>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alabang"] = Barangay.Alabang,
            ["Ayala Alabang"] = Barangay.Ayala_Alabang,
            ["Sucat"] = Barangay.Sucat,
            ["Poblacion"] = Barangay.Poblacion,
            ["Putatan"] = Barangay.Putatan,
            ["Tunasan"] = Barangay.Tunasan,
            ["Cupang"] = Barangay.Cupang,
            ["Bayanan"] = Barangay.Bayanan,
            ["Buli"] = Barangay.Buli,
        };

        Boundaries = new();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory)!.Parent!.Parent!.FullName.Replace("bin", "");
        var geojsonPath = Path.Combine(projectDirectory, "JsonFiles", "precincts.geojson");
        var json = File.ReadAllText(geojsonPath);

        using var doc = JsonDocument.Parse(json);
        var features = doc.RootElement.GetProperty("features");

        foreach (var feature in features.EnumerateArray())
        {
            var name = feature.GetProperty("properties").GetProperty("name").GetString()!;
            if (!nameToBarangay.TryGetValue(name, out var barangay))
                continue;

            var ring = feature.GetProperty("geometry").GetProperty("coordinates")[0];
            var boundary = new List<(double Lng, double Lat)>();
            var minLat = 90.0;
            var maxLat = -90.0;
            var minLng = 180.0;
            var maxLng = -180.0;

            foreach (var point in ring.EnumerateArray())
            {
                var lng = point[0].GetDouble();
                var lat = point[1].GetDouble();
                boundary.Add((lng, lat));
                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lng < minLng) minLng = lng;
                if (lng > maxLng) maxLng = lng;
            }

            Boundaries[barangay] = (minLat, maxLat, minLng, maxLng, boundary);
        }
    }

    public static (double Latitude, double Longitude) GetRandomPoint(Barangay barangay)
    {
        if (!Boundaries.TryGetValue(barangay, out var bounds))
            return (0, 0);

        const int maxAttempts = 50;
        for (var i = 0; i < maxAttempts; i++)
        {
            var lat = bounds.MinLat + Rng.NextDouble() * (bounds.MaxLat - bounds.MinLat);
            var lng = bounds.MinLng + Rng.NextDouble() * (bounds.MaxLng - bounds.MinLng);

            if (IsPointInPolygon(lat, lng, bounds.Boundary))
                return (Math.Round(lat, 6), Math.Round(lng, 6));
        }

        var first = bounds.Boundary[0];
        return (Math.Round(first.Lat, 6), Math.Round(first.Lng, 6));
    }

    private static bool IsPointInPolygon(double lat, double lng, List<(double Lng, double Lat)> polygon)
    {
        var inside = false;
        var j = polygon.Count - 1;
        for (var i = 0; i < polygon.Count; i++)
        {
            if ((polygon[i].Lat > lat) != (polygon[j].Lat > lat) &&
                lng < (polygon[j].Lng - polygon[i].Lng) * (lat - polygon[i].Lat) / (polygon[j].Lat - polygon[i].Lat) + polygon[i].Lng)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
