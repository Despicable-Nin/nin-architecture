using espasyo.Application.Common.Models.ML;
using espasyo.Application.Interfaces;

namespace espasyo.Infrastructure.MachineLearning;

public class SeasonalForecastService(
    ILogger<SeasonalForecastService> logger
) : ISeasonalForecastService
{
    public Task<List<SeasonalPredictionRow>> PredictSeasonal(IEnumerable<ClusterGroup> clusterData, ForecastParameters parameters)
    {
        return Task.Run(() =>
        {
            logger.LogInformation("Running seasonal decomposition");

            var allItems = clusterData.SelectMany(g => g.ClusterItems).ToList();

            // Group historical data by (precinct, crimeType) ordered by month
            var groups = allItems
                .GroupBy(i => (Precinct: (int)i.Precinct, CrimeType: (int)i.CrimeType))
                .ToList();

            var results = new List<SeasonalPredictionRow>();

            foreach (var group in groups)
            {
                var monthly = group
                    .GroupBy(i => new { i.Year, i.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = (double)g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                if (monthly.Count < 12) continue;

                var counts = monthly.Select(x => x.Count).ToList();
                var n = counts.Count;

                // Linear regression trend: y = a + b*x
                var indices = Enumerable.Range(0, n).Select(i => (double)i).ToList();
                var xMean = indices.Average();
                var yMean = counts.Average();
                var b = indices.Zip(counts, (x, y) => (x - xMean) * (y - yMean)).Sum()
                       / indices.Sum(x => (x - xMean) * (x - xMean));
                var a = yMean - b * xMean;
                var trend = indices.Select(x => a + b * x).ToList();

                // Detrend: actual / trend (multiplicative)
                var detrended = counts.Select((c, i) => trend[i] > 0 ? c / trend[i] : 1.0).ToList();

                // Seasonal indices: average by month
                var seasonalByMonth = new double[12];
                var monthCounts = new int[12];
                for (int i = 0; i < n; i++)
                {
                    var m = monthly[i].Month - 1;
                    seasonalByMonth[m] += detrended[i];
                    monthCounts[m]++;
                }
                for (int m = 0; m < 12; m++)
                {
                    seasonalByMonth[m] = monthCounts[m] > 0 ? seasonalByMonth[m] / monthCounts[m] : 1.0;
                }

                // Normalize so average is 1.0
                var avgSeasonal = seasonalByMonth.Average();
                if (avgSeasonal > 0)
                {
                    for (int m = 0; m < 12; m++)
                        seasonalByMonth[m] /= avgSeasonal;
                }

                // Residual = detrended / seasonalIndex
                var residual = detrended.Select((d, i) =>
                {
                    var si = seasonalByMonth[monthly[i].Month - 1];
                    return si > 0 ? d / si : 1.0;
                }).ToList();

                // Strength: 1 - var(residual) / var(detrended)
                var varDetrended = Variance(detrended);
                var varResidual = Variance(residual);
                var seasonalStrength = varDetrended > 0 ? 1.0 - varResidual / varDetrended : 0;

                // Peak/trough months from seasonal indices
                var peakMonth = Array.IndexOf(seasonalByMonth, seasonalByMonth.Max()) + 1;
                var troughMonth = Array.IndexOf(seasonalByMonth, seasonalByMonth.Min()) + 1;

                results.Add(new SeasonalPredictionRow
                {
                    Precinct = group.Key.Precinct,
                    CrimeType = group.Key.CrimeType,
                    Trend = [.. trend],
                    Seasonal = [.. seasonalByMonth],
                    Residual = [.. residual],
                    Strength = new Dictionary<string, double>
                    {
                        { "trend", Math.Abs(b) },
                        { "seasonal", Math.Min(seasonalStrength, 1.0) }
                    },
                    PeakMonth = peakMonth,
                    TroughMonth = troughMonth
                });
            }

            logger.LogInformation("Decomposed {Count} series", results.Count);
            return results;
        });
    }

    private static double Variance(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        return values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
    }
}
