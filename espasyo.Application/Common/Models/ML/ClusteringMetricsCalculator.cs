namespace espasyo.Application.Common.Models.ML;

public static class ClusteringMetricsCalculator
{
    public static ClusterQualityMetrics Compute(
        List<(double[] features, uint clusterId)> assignments,
        int selectedK)
    {
        var scores = new Dictionary<int, (double sil, double dbi, double ch)>();

        var distinctK = assignments.Select(a => (int)a.clusterId + 1).Distinct().OrderBy(k => k).ToList();
        var testedK = Enumerable.Range(2, 14).Where(k => k <= distinctK.Max()).ToList();

        foreach (var k in testedK)
        {
            var clusterIds = Enumerable.Range(0, k).Select(i => (uint)i).ToHashSet();
            var filtered = assignments.Where(a => clusterIds.Contains(a.clusterId)).ToList();
            if (filtered.Count < k * 2) continue;

            var sil = ComputeSilhouette(filtered, k);
            var dbi = ComputeDaviesBouldin(filtered, k);
            var ch = ComputeCalinskiHarabasz(filtered, k);

            scores[k] = (sil, dbi, ch);
        }

        if (scores.Count == 0)
            return new ClusterQualityMetrics { SelectedK = selectedK, OptimalK = selectedK };

        var bestK = scores.OrderByDescending(s => s.Value.sil).First().Key;

        return new ClusterQualityMetrics
        {
            OptimalK = bestK,
            SelectedK = selectedK,
            SilhouetteScores = scores.ToDictionary(s => s.Key, s => s.Value.sil),
            DaviesBouldinScores = scores.ToDictionary(s => s.Key, s => s.Value.dbi),
            CalinskiHarabaszScores = scores.ToDictionary(s => s.Key, s => s.Value.ch)
        };
    }

    private static double ComputeSilhouette(List<(double[] features, uint clusterId)> data, int k)
    {
        var centroids = ComputeCentroids(data, k);
        var overall = 0.0;
        var count = 0;

        foreach (var (features, clusterId) in data)
        {
            var a = EuclideanDistance(features, centroids[(int)clusterId]);

            var b = double.MaxValue;
            for (uint j = 0; j < (uint)k; j++)
            {
                if (j == clusterId) continue;
                var d = EuclideanDistance(features, centroids[(int)j]);
                if (d < b) b = d;
            }

            var s = (b - a) / Math.Max(a, b);
            overall += s;
            count++;
        }

        return count > 0 ? overall / count : 0;
    }

    private static double ComputeDaviesBouldin(List<(double[] features, uint clusterId)> data, int k)
    {
        var centroids = ComputeCentroids(data, k);
        var scatter = new double[k];

        for (uint i = 0; i < (uint)k; i++)
        {
            var clusterPoints = data.Where(d => d.clusterId == i).Select(d => d.features).ToList();
            if (clusterPoints.Count == 0) continue;
            scatter[i] = clusterPoints.Average(p => EuclideanDistance(p, centroids[(int)i]));
        }

        var total = 0.0;
        for (uint i = 0; i < (uint)k; i++)
        {
            var maxRatio = 0.0;
            for (uint j = 0; j < (uint)k; j++)
            {
                if (i == j) continue;
                var m = EuclideanDistance(centroids[(int)i], centroids[(int)j]);
                if (m < 1e-10) continue;
                var ratio = (scatter[i] + scatter[j]) / m;
                if (ratio > maxRatio) maxRatio = ratio;
            }
            total += maxRatio;
        }

        return k > 0 ? total / k : 0;
    }

    private static double ComputeCalinskiHarabasz(List<(double[] features, uint clusterId)> data, int k)
    {
        var centroids = ComputeCentroids(data, k);
        var overallCentroid = OverallCentroid(data);

        var betweenSs = 0.0;
        var withinSs = 0.0;
        var n = data.Count;

        for (uint i = 0; i < (uint)k; i++)
        {
            var clusterPoints = data.Where(d => d.clusterId == i).Select(d => d.features).ToList();
            if (clusterPoints.Count == 0) continue;

            betweenSs += clusterPoints.Count * SquaredDistance(centroids[(int)i], overallCentroid);
            withinSs += clusterPoints.Sum(p => SquaredDistance(p, centroids[(int)i]));
        }

        if (withinSs < 1e-10 || k <= 1 || n <= k) return 0;
        return (betweenSs / (k - 1)) / (withinSs / (n - k));
    }

    private static double[][] ComputeCentroids(List<(double[] features, uint clusterId)> data, int k)
    {
        var centroids = new double[k][];
        var counts = new int[k];
        var dim = data[0].features.Length;

        for (var i = 0; i < k; i++)
            centroids[i] = new double[dim];

        foreach (var (features, clusterId) in data)
        {
            for (var d = 0; d < dim; d++)
                centroids[clusterId][d] += features[d];
            counts[clusterId]++;
        }

        for (var i = 0; i < k; i++)
        {
            if (counts[i] > 0)
            {
                for (var d = 0; d < dim; d++)
                    centroids[i][d] /= counts[i];
            }
        }

        return centroids;
    }

    private static double[] OverallCentroid(List<(double[] features, uint clusterId)> data)
    {
        if (data.Count == 0) return [];
        var dim = data[0].features.Length;
        var centroid = new double[dim];
        foreach (var (features, _) in data)
        {
            for (var d = 0; d < dim; d++)
                centroid[d] += features[d];
        }
        for (var d = 0; d < dim; d++)
            centroid[d] /= data.Count;
        return centroid;
    }

    private static double EuclideanDistance(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    private static double SquaredDistance(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }
        return sum;
    }
}
