using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;

namespace espasyo.Infrastructure.MachineLearning;

public class SpatialForecastService(
    ILogger<SpatialForecastService> logger
) : ISpatialForecastService
{
    public Task<List<SpatialForecastRow>> DistributeForecast(
        IEnumerable<ClusterGroup> clusterData,
        ForecastParameters parameters,
        List<ForecastSeries> temporalSeries)
    {
        return Task.Run(() =>
        {
            logger.LogInformation("Distributing temporal forecast across {ClusterCount} clusters", clusterData.Count());

            var clusterList = clusterData.ToList();
            var allItems = clusterList.SelectMany(g => g.ClusterItems).ToList();

            // Build historical counts per (precinct, clusterId)
            var historicalCounts = allItems
                .GroupBy(i => (Precinct: (int)i.Precinct, ClusterId: i.ClusterId))
                .ToDictionary(g => g.Key, g => g.Count());

            // Build total per precinct
            var precinctTotals = allItems
                .GroupBy(i => (int)i.Precinct)
                .ToDictionary(g => g.Key, g => g.Count());

            // Build precinct-specific centroid lookup
            var precinctCentroids = allItems
                .GroupBy(i => (Precinct: (int)i.Precinct, ClusterId: i.ClusterId))
                .ToDictionary(g => g.Key, g => (
                    Lat: g.Average(item => item.Latitude),
                    Lon: g.Average(item => item.Longitude)
                ));

            var spatialRows = new List<SpatialForecastRow>();

            foreach (var series in temporalSeries)
            {
                var precinct = series.Precinct;
                if (!precinctTotals.TryGetValue(precinct, out var precinctTotal) || precinctTotal == 0)
                    continue;

                // Find clusters that have incidents in this precinct
                var precinctClusters = historicalCounts
                    .Where(kvp => kvp.Key.Precinct == precinct)
                    .ToList();

                if (precinctClusters.Count == 0)
                    continue;

                foreach (var point in series.Forecasts)
                {
                    foreach (var (key, count) in precinctClusters)
                    {
                        var proportion = (double)count / precinctTotal;
                        var hasCentroid = precinctCentroids.TryGetValue((precinct, key.ClusterId), out var centroid);

                        spatialRows.Add(new SpatialForecastRow
                        {
                            Precinct = precinct,
                            ClusterId = key.ClusterId,
                            Latitude = hasCentroid ? centroid.Lat : null,
                            Longitude = hasCentroid ? centroid.Lon : null,
                            Timestamp = point.Timestamp,
                            Forecast = point.Forecast * proportion,
                            LowerBound = point.LowerBound * proportion,
                            UpperBound = point.UpperBound * proportion,
                            Confidence = point.Confidence,
                            Trend = point.Trend,
                            RiskLevel = point.RiskLevel
                        });
                    }
                }
            }

            logger.LogInformation("Generated {RowCount} spatial forecast rows", spatialRows.Count);
            return spatialRows;
        });
    }
}
