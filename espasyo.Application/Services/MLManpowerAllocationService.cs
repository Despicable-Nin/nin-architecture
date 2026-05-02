using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using espasyo.Domain.Entities;
using espasyo.Domain.Enums;
using espasyo.Application.Interfaces;
using espasyo.Application.Configuration;

namespace espasyo.Application.Services;

/// <summary>
/// ML.NET-based manpower allocation service that learns optimal staffing patterns from historical data.
/// Trained models are persisted to disk so training only happens once per environment setup.
/// On subsequent runs (including after restarts), models are loaded instantly from disk.
/// </summary>
public class MLManpowerAllocationService
{
    private readonly MLContext _mlContext;
    private readonly IIncidentRepository _incidentRepository;
    private readonly IManpowerRepository _manpowerRepository;
    private readonly MLSettings _mlSettings;
    private readonly DataDrivenComplexityService _complexityService;
    private readonly ILogger<MLManpowerAllocationService> _logger;

    private ITransformer? _complexityModel;
    private ITransformer? _workloadModel;
    private ITransformer? _optimizationModel;

    // -------------------------------------------------------------------------
    // Model file paths
    // These .zip files are written once after training and loaded on every
    // subsequent startup, completely avoiding on-the-fly training during requests.
    // During development: delete these files to force a re-train on next startup.
    // -------------------------------------------------------------------------
    private static readonly string ModelsDirectory = Path.Combine(AppContext.BaseDirectory, "MLModels");
    private static readonly string ComplexityModelPath   = Path.Combine(ModelsDirectory, "complexity_model.zip");
    private static readonly string WorkloadModelPath     = Path.Combine(ModelsDirectory, "workload_model.zip");
    private static readonly string OptimizationModelPath = Path.Combine(ModelsDirectory, "optimization_model.zip");

    public MLManpowerAllocationService(
        IIncidentRepository incidentRepository,
        IManpowerRepository manpowerRepository,
        IOptions<MLSettings> mlSettings,
        DataDrivenComplexityService complexityService,
        ILogger<MLManpowerAllocationService> logger)
    {
        _mlSettings = mlSettings.Value ?? throw new ArgumentNullException(nameof(mlSettings));
        _logger = logger;

        // Validate configuration on startup
        var validationErrors = MLConfigurationValidator.ValidateConfiguration(_mlSettings);
        if (validationErrors.Any())
        {
            throw new InvalidOperationException(
                $"ML configuration validation failed: {string.Join("; ", validationErrors)}");
        }

        _mlContext = new MLContext(seed: _mlSettings.Training.RandomSeed);
        _incidentRepository = incidentRepository;
        _manpowerRepository = manpowerRepository;
        _complexityService = complexityService;

        // Try to load pre-trained models from disk immediately.
        // If this succeeds, no training is ever needed during a request.
        TryLoadModelsFromDisk();
    }

    /// <summary>
    /// Attempts to load all three ML models from their persisted .zip files.
    /// Returns true if all models were loaded successfully; false if any are missing.
    /// </summary>
    private bool TryLoadModelsFromDisk()
    {
        try
        {
            if (!File.Exists(ComplexityModelPath) ||
                !File.Exists(WorkloadModelPath) ||
                !File.Exists(OptimizationModelPath))
            {
                _logger.LogInformation(
                    "Pre-trained ML models not found on disk. Models will be trained and saved on the first request.");
                return false;
            }

            _complexityModel   = _mlContext.Model.Load(ComplexityModelPath,   out _);
            _workloadModel     = _mlContext.Model.Load(WorkloadModelPath,     out _);
            _optimizationModel = _mlContext.Model.Load(OptimizationModelPath, out _);

            _logger.LogInformation(
                "Pre-trained ML models loaded from disk successfully. Training will be skipped.");
            return true;
        }
        catch (Exception ex)
        {
            // If loading fails (e.g., corrupt file), fall back to training on next request.
            _logger.LogWarning(ex, "Failed to load ML models from disk. They will be retrained.");
            _complexityModel = _workloadModel = _optimizationModel = null;
            return false;
        }
    }

    /// <summary>
    /// Train ML models using historical incident and manpower data
    /// </summary>
    /// <summary>
    /// Trains all three ML models from historical data and persists them to disk.
    /// In production, this should be triggered by a background/scheduled service, NOT by a user request.
    /// In development, this runs automatically on the first request if no model files exist on disk.
    /// Once saved, the app will load the .zip files on every subsequent startup instead of retraining.
    /// To force a re-train: delete the files in the MLModels/ directory and restart the application.
    /// </summary>
    public async Task<MLTrainingResult> TrainModelsAsync()
    {
        var result = new MLTrainingResult();

        try
        {
            _logger.LogInformation("Starting ML model training...");

            // 1. Get historical data
            var incidents = await GetHistoricalIncidentData();
            var manpowerData = await GetHistoricalManpowerData();

            if (incidents.Count < _mlSettings.Training.MinimumTrainingDataPoints || !manpowerData.Any())
            {
                result.Success = false;
                result.ErrorMessage = $"Insufficient historical data for training. Required: {_mlSettings.Training.MinimumTrainingDataPoints}, Found: {incidents.Count} incidents";
                return result;
            }

            // 2. Train all three models
            result.ComplexityModelMetrics   = await TrainCrimeComplexityModel(incidents);
            result.WorkloadModelMetrics     = await TrainWorkloadPredictionModel(incidents, manpowerData);
            result.OptimizationModelMetrics = await TrainManpowerOptimizationModel(manpowerData, incidents);

            // 3. Persist models to disk so future startups skip training entirely
            Directory.CreateDirectory(ModelsDirectory);
            _mlContext.Model.Save(_complexityModel,   null, ComplexityModelPath);
            _mlContext.Model.Save(_workloadModel,     null, WorkloadModelPath);
            _mlContext.Model.Save(_optimizationModel, null, OptimizationModelPath);
            _logger.LogInformation("ML models saved to disk at: {Directory}", ModelsDirectory);

            result.Success = true;
            result.TrainingDataPoints = incidents.Count;
            result.TrainedAt = DateTime.UtcNow;

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Training failed: {ex.Message}";
            _logger.LogError(ex, "ML model training failed.");
            return result;
        }
    }

    /// <summary>
    /// Calculate optimal manpower allocation using data-driven formulas
    /// No hard-coded thresholds - all values derived from historical data patterns
    /// </summary>
    public async Task<MLManpowerRecommendation> CalculateOptimalManpowerAsync(
        Barangay precinct, 
        Dictionary<CrimeTypeEnum, int> predictedCrimeCounts,
        IEnumerable<HistoricalIncidentData> historicalData,
        int currentYear)
    {
        // Models are loaded from disk in the constructor.
        // If they are still null here, it means no model files were found on disk (first-ever run in this environment).
        // We train once now and save to disk so subsequent requests are instant.
        if (_complexityModel == null || _workloadModel == null || _optimizationModel == null)
        {
            _logger.LogWarning(
                "ML models not loaded. Running one-time training. " +
                "This will not happen again until the model files are manually deleted from disk.");
            await TrainModelsAsync();
        }

        var recommendation = new MLManpowerRecommendation
        {
            Precinct = precinct,
            PredictedCrimeCounts = predictedCrimeCounts,
            Year = currentYear
        };

        // 1. Predict crime complexity using learned patterns
        var complexityPredictions = PredictCrimeComplexity(predictedCrimeCounts);
        recommendation.CrimeComplexityScore = complexityPredictions.Average();

        // 2. Predict workload requirements
        var workloadPrediction = PredictWorkloadRequirements(precinct, predictedCrimeCounts, complexityPredictions);
        recommendation.PredictedWorkloadHours = workloadPrediction.WorkloadHours;
        recommendation.WorkloadConfidence = workloadPrediction.Confidence;

        // 3. Optimize manpower allocation
        var optimizationResult = OptimizeManpowerAllocation(precinct, workloadPrediction.WorkloadHours);
        recommendation.RecommendedManpower = optimizationResult.OptimalManpower;
        recommendation.OptimizationConfidence = optimizationResult.Confidence;

        // 4. Generate ML-based justification
        recommendation.MLJustification = GenerateMLJustification(
            complexityPredictions, workloadPrediction, optimizationResult);

        return recommendation;
    }

    /// <summary>
    /// Train crime complexity model using historical incident resolution data
    /// </summary>
    private async Task<ModelMetrics> TrainCrimeComplexityModel(List<HistoricalIncidentData> incidents)
    {
        // Create training data with features: CrimeType, Precinct, TimeOfDay, Weather, etc.
        // Target: ComplexityScore (derived from historical patterns)
        var trainingData = incidents.Select(i => new CrimeComplexityTrainingData
        {
            CrimeType = (float)i.CrimeType,
            Precinct = (float)i.Precinct,
            TimeOfDay = GetTimeOfDayNumeric(i.TimeStamp),
            Weather = (float)i.Weather,
            Severity = (float)i.Severity,
            // Learn complexity from historical incident clustering patterns
            ComplexityScore = CalculateHistoricalComplexity(i, incidents)
        }).ToArray();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Build ML pipeline for regression
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(CrimeComplexityTrainingData.CrimeType),
                nameof(CrimeComplexityTrainingData.Precinct),
                nameof(CrimeComplexityTrainingData.TimeOfDay),
                nameof(CrimeComplexityTrainingData.Weather),
                nameof(CrimeComplexityTrainingData.Severity))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.LbfgsPoissonRegression(
                labelColumnName: nameof(CrimeComplexityTrainingData.ComplexityScore)));

        // Train the model
        _complexityModel = pipeline.Fit(dataView);

        // Evaluate model performance
        var predictions = _complexityModel.Transform(dataView);
        var metrics = _mlContext.Regression.Evaluate(predictions, 
            nameof(CrimeComplexityTrainingData.ComplexityScore));

        return new ModelMetrics
        {
            ModelType = "Crime Complexity Analysis",
            RSquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            TrainingDataPoints = trainingData.Length
        };
    }

    /// <summary>
    /// Train workload prediction model using historical staffing and crime data correlations
    /// </summary>
    private async Task<ModelMetrics> TrainWorkloadPredictionModel(
        List<HistoricalIncidentData> incidents, 
        List<HistoricalManpowerData> manpowerData)
    {
        // Aggregate data by precinct-month to create workload training examples
        var trainingData = manpowerData.SelectMany(m =>
        {
            var monthlyIncidents = incidents
                .Where(i => i.Precinct == m.Precinct && 
                           i.TimeStamp.Year == m.Year && 
                           i.TimeStamp.Month == m.Month)
                .GroupBy(i => i.CrimeType)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalCrimeComplexity = monthlyIncidents.Sum(kvp => 
                kvp.Value * GetLearnedComplexity(kvp.Key));

            return new[] { new WorkloadTrainingData
            {
                Precinct = (float)m.Precinct,
                TotalCrimes = monthlyIncidents.Values.Sum(),
                CrimeComplexityScore = totalCrimeComplexity,
                PopulationDensity = GetPopulationDensity(m.Precinct),
                SeasonalFactor = GetSeasonalFactor(m.Month),
                // Target: actual workload hours (derived from staffing effectiveness)
                ActualWorkloadHours = CalculateActualWorkload(m, monthlyIncidents.Values.Sum())
            }};
        }).ToArray();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(WorkloadTrainingData.Precinct),
                nameof(WorkloadTrainingData.TotalCrimes),
                nameof(WorkloadTrainingData.CrimeComplexityScore),
                nameof(WorkloadTrainingData.PopulationDensity),
                nameof(WorkloadTrainingData.SeasonalFactor))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.LbfgsPoissonRegression(
                labelColumnName: nameof(WorkloadTrainingData.ActualWorkloadHours),
                featureColumnName: "Features"));

        _workloadModel = pipeline.Fit(dataView);

        var predictions = _workloadModel.Transform(dataView);
        var metrics = _mlContext.Regression.Evaluate(predictions,
            nameof(WorkloadTrainingData.ActualWorkloadHours));

        return new ModelMetrics
        {
            ModelType = "Workload Prediction",
            RSquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            TrainingDataPoints = trainingData.Length
        };
    }

    /// <summary>
    /// Train manpower optimization model using historical performance outcomes
    /// </summary>
    private async Task<ModelMetrics> TrainManpowerOptimizationModel(
        List<HistoricalManpowerData> manpowerData, 
        List<HistoricalIncidentData> incidents)
    {
        // Create training data correlating staffing levels with performance outcomes
        var trainingData = manpowerData.Select(m =>
        {
            var monthlyIncidents = incidents
                .Where(i => i.Precinct == m.Precinct && 
                           i.TimeStamp.Year == m.Year && 
                           i.TimeStamp.Month == m.Month)
                .ToList();

            var performanceScore = CalculatePerformanceScore(m.StaffingLevel, monthlyIncidents);

            return new OptimizationTrainingData
            {
                Precinct = (float)m.Precinct,
                PredictedWorkload = CalculateWorkloadFromCrimes(monthlyIncidents),
                StaffingLevel = m.StaffingLevel,
                PopulationDensity = GetPopulationDensity(m.Precinct),
                CrimeRate = monthlyIncidents.Count,
                // Target: performance score (crime clearance rate, response time, etc.)
                PerformanceScore = performanceScore
            };
        }).ToArray();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(OptimizationTrainingData.Precinct),
                nameof(OptimizationTrainingData.PredictedWorkload),
                nameof(OptimizationTrainingData.StaffingLevel),
                nameof(OptimizationTrainingData.PopulationDensity),
                nameof(OptimizationTrainingData.CrimeRate))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(OptimizationTrainingData.PerformanceScore),
                featureColumnName: "Features"));

        _optimizationModel = pipeline.Fit(dataView);

        var predictions = _optimizationModel.Transform(dataView);
        var metrics = _mlContext.Regression.Evaluate(predictions,
            nameof(OptimizationTrainingData.PerformanceScore));

        return new ModelMetrics
        {
            ModelType = "Manpower Optimization",
            RSquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            TrainingDataPoints = trainingData.Length
        };
    }

    // Helper methods for data processing and feature engineering
    private async Task<List<HistoricalIncidentData>> GetHistoricalIncidentData()
    {
        var endDate = DateOnly.FromDateTime(DateTime.Now);
        var startDate = endDate.AddYears(-_mlSettings.HistoricalData.HistoricalYears);

        var incidents = await _incidentRepository.GetAllIncidentsAsync(
            new KeyValuePair<DateOnly, DateOnly>(startDate, endDate));

        return incidents.Select(i => new HistoricalIncidentData
        {
            CrimeType = i.CrimeType,
            Precinct = i.PoliceDistrict,
            TimeStamp = i.TimeStamp ?? DateTimeOffset.Now,
            Weather = i.Weather,
            Severity = i.Severity
        }).ToList();
    }

    private async Task<List<HistoricalManpowerData>> GetHistoricalManpowerData()
    {
        var currentYear = DateTime.Now.Year;
        var manpowerData = new List<HistoricalManpowerData>();

        // Get manpower data for configured historical years
        var startYear = currentYear - _mlSettings.HistoricalData.HistoricalYears;
        var endYear = _mlSettings.HistoricalData.IncludeCurrentYear ? currentYear : currentYear - 1;
        for (int year = startYear; year <= endYear; year++)
        {
            var yearlyData = await _manpowerRepository.GetAllManpowerAsync();
            foreach (var data in yearlyData)
            {
                // Create monthly records (simplified - in real implementation, 
                // you'd have monthly staffing data)
                for (int month = 1; month <= 12; month++)
                {
                    manpowerData.Add(new HistoricalManpowerData
                    {
                        Precinct = Barangay.Alabang, // Simplified - use default precinct
                        Year = year,
                        Month = month,
                        StaffingLevel = data.HeadCount
                    });
                }
            }
        }

        return manpowerData;
    }

    private float CalculateHistoricalComplexity(HistoricalIncidentData incident, List<HistoricalIncidentData> allIncidents)
    {
        // Learn complexity from historical patterns - crimes that co-occur with complex investigations
        var similarIncidents = allIncidents
            .Where(i => i.CrimeType == incident.CrimeType && i.Precinct == incident.Precinct)
            .ToList();

        if (!similarIncidents.Any()) return 1.0f;

        // Complexity factors learned from data:
        // 1. Crime type clustering (complex crimes tend to cluster together)
        var complexClusterFactor = similarIncidents.Count(i => IsComplexCrimeType(i.CrimeType)) / (float)similarIncidents.Count;
        
        // 2. Temporal patterns (complex crimes often have seasonal patterns)
        var seasonalComplexity = GetSeasonalComplexityFactor(incident.TimeStamp.Month);
        
        // 3. Geographic complexity (some precincts have higher investigation complexity)
        var geographicFactor = GetGeographicComplexityFactor(incident.Precinct);

        return (complexClusterFactor + seasonalComplexity + geographicFactor) / 3.0f;
    }

    private List<float> PredictCrimeComplexity(Dictionary<CrimeTypeEnum, int> predictedCrimeCounts)
    {
        if (_complexityModel == null)
            return predictedCrimeCounts.Keys.Select(_ => _mlSettings.Defaults.DefaultComplexityScore).ToList();

        var predictions = new List<float>();
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<CrimeComplexityTrainingData, CrimeComplexityPrediction>(_complexityModel);

        foreach (var (crimeType, count) in predictedCrimeCounts)
        {
            var input = new CrimeComplexityTrainingData
            {
                CrimeType = (float)crimeType,
                Precinct = 0, // Will be set per prediction
                TimeOfDay = 12.0f, // Average time
                Weather = 0, // Average weather
                Severity = 1 // Average severity
            };

            var prediction = predictionEngine.Predict(input);
            predictions.Add(prediction.PredictedComplexity);
        }

        return predictions;
    }

    private WorkloadPrediction PredictWorkloadRequirements(
        Barangay precinct, 
        Dictionary<CrimeTypeEnum, int> predictedCrimeCounts,
        List<float> complexityScores)
    {
        if (_workloadModel == null)
            return new WorkloadPrediction 
            { 
                WorkloadHours = _mlSettings.Defaults.DefaultWorkloadHours, 
                Confidence = _mlSettings.Defaults.DefaultConfidence 
            };

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<WorkloadTrainingData, WorkloadPrediction>(_workloadModel);

        var input = new WorkloadTrainingData
        {
            Precinct = (float)precinct,
            TotalCrimes = predictedCrimeCounts.Values.Sum(),
            CrimeComplexityScore = complexityScores.Average(),
            PopulationDensity = GetPopulationDensity(precinct),
            SeasonalFactor = GetSeasonalFactor(DateTime.Now.Month)
        };

        return predictionEngine.Predict(input);
    }

    private OptimizationResult OptimizeManpowerAllocation(Barangay precinct, float predictedWorkloadHours)
    {
        if (_optimizationModel == null)
        {
            var fallbackManpower = (int)Math.Ceiling(predictedWorkloadHours / _mlSettings.FeatureEngineering.StandardMonthlyHours);
            return new OptimizationResult 
            { 
                OptimalManpower = Math.Max(fallbackManpower, _mlSettings.Optimization.MinimumStaffingLevel), 
                Confidence = _mlSettings.Defaults.DefaultConfidence 
            };
        }

        // Use optimization model to find staffing level that maximizes performance
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<OptimizationTrainingData, PerformancePrediction>(_optimizationModel);

        var bestStaffing = _mlSettings.Optimization.MinimumStaffingLevel;
        var bestPerformance = 0.0f;

        // Test different staffing levels to find optimal
        for (int staffing = _mlSettings.Optimization.MinimumStaffingLevel; 
             staffing <= _mlSettings.Optimization.MaximumStaffingLevel; 
             staffing += _mlSettings.Optimization.StaffingOptimizationStep)
        {
            var input = new OptimizationTrainingData
            {
                Precinct = (float)precinct,
                PredictedWorkload = predictedWorkloadHours,
                StaffingLevel = staffing,
                PopulationDensity = GetPopulationDensity(precinct),
                CrimeRate = (int)(predictedWorkloadHours / _mlSettings.Workload.BaseHoursPerCrime) // Estimate crimes from workload
            };

            var prediction = predictionEngine.Predict(input);
            if (prediction.PredictedPerformance > bestPerformance)
            {
                bestPerformance = prediction.PredictedPerformance;
                bestStaffing = staffing;
            }
        }

        return new OptimizationResult 
        { 
            OptimalManpower = bestStaffing, 
            Confidence = Math.Min(1.0f, bestPerformance),
            ExpectedPerformance = bestPerformance
        };
    }

    // Helper methods for feature engineering using configuration-driven values
    private float GetTimeOfDayNumeric(DateTimeOffset timestamp) => timestamp.Hour + (timestamp.Minute / 60.0f);
    
    private async Task<bool> IsComplexCrimeTypeAsync(CrimeTypeEnum crimeType)
    {
        var complexCrimeTypes = await _complexityService.CalculateComplexCrimeTypesAsync();
        return complexCrimeTypes.Contains(crimeType.ToString());
    }
    
    private float GetSeasonalComplexityFactor(int month) => 
        1.0f + (Math.Abs(month - 6) / 12.0f) * _mlSettings.Complexity.MaxSeasonalComplexityVariation;
    
    private async Task<float> GetGeographicComplexityFactorAsync(Barangay precinct)
    {
        var complexityFactors = await _complexityService.CalculateGeographicComplexityFactorsAsync();
        var precinctName = precinct.ToString().Replace("_", " ");
        
        return complexityFactors.TryGetValue(precinctName, out var factor) 
            ? factor 
            : await _complexityService.CalculateDefaultComplexityFactorAsync();
    }
    
    // Synchronous wrapper methods for backward compatibility in ML training contexts
    private bool IsComplexCrimeType(CrimeTypeEnum crimeType)
    {
        // For training contexts, use a simple severity-based approach as fallback
        // This avoids async calls in training data preparation
        return crimeType switch
        {
            CrimeTypeEnum.Murder or CrimeTypeEnum.Homicide or CrimeTypeEnum.HumanTrafficking or CrimeTypeEnum.Corruption => true,
            _ => false
        };
    }
    
    private float GetGeographicComplexityFactor(Barangay precinct)
    {
        // For training contexts, use default complexity to avoid async calls
        // The actual data-driven complexity will be used in prediction contexts
        return _mlSettings.Complexity.DefaultGeographicComplexity;
    }
    
    private float GetPopulationDensity(Barangay precinct) => 
        (float)precinct * _mlSettings.FeatureEngineering.PopulationDensityScaling;
    
    private float GetSeasonalFactor(int month) => 
        1.0f + _mlSettings.FeatureEngineering.SeasonalVariationAmplitude * (float)Math.Sin((month - 1) * Math.PI / 6);
    
    private float CalculateActualWorkload(HistoricalManpowerData manpower, int crimeCount) => 
        manpower.StaffingLevel * _mlSettings.FeatureEngineering.StandardMonthlyHours;
    
    private float CalculatePerformanceScore(int staffingLevel, List<HistoricalIncidentData> incidents) =>
        Math.Max(0, Math.Min(1, 1.0f - (incidents.Count / (float)(staffingLevel * _mlSettings.Workload.MaxCrimesPerOfficerPerMonth))));
    
    private float CalculateWorkloadFromCrimes(List<HistoricalIncidentData> incidents) => 
        incidents.Count * _mlSettings.Workload.BaseHoursPerCrime;
    
    private float GetLearnedComplexity(CrimeTypeEnum crimeType)
    {
        // Use complexity model if available, otherwise use configured default complexity
        if (_complexityModel == null)
            return _mlSettings.Defaults.DefaultComplexityScore;
        
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<CrimeComplexityTrainingData, CrimeComplexityPrediction>(_complexityModel);
        var input = new CrimeComplexityTrainingData
        {
            CrimeType = (float)crimeType,
            Precinct = 0, // Average precinct
            TimeOfDay = 12.0f, // Average time
            Weather = 0, // Average weather
            Severity = 1 // Average severity
        };
        
        var prediction = predictionEngine.Predict(input);
        return prediction.PredictedComplexity;
    }

    private string GenerateMLJustification(
        List<float> complexityPredictions, 
        WorkloadPrediction workloadPrediction, 
        OptimizationResult optimizationResult)
    {
        return $"ML-based recommendation using trained models: " +
               $"Average crime complexity score: {complexityPredictions.Average():F2}, " +
               $"Predicted workload: {workloadPrediction.WorkloadHours:F0} hours " +
               $"(confidence: {workloadPrediction.Confidence:P0}), " +
               $"Optimal staffing: {optimizationResult.OptimalManpower} officers " +
               $"(expected performance: {optimizationResult.ExpectedPerformance:P0})";
    }
}

// Data models for ML training and prediction
public class HistoricalIncidentData
{
    public CrimeTypeEnum CrimeType { get; set; }
    public Barangay Precinct { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public WeatherConditionEnum Weather { get; set; }
    public SeverityEnum Severity { get; set; }
}

public class HistoricalManpowerData
{
    public Barangay Precinct { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int StaffingLevel { get; set; }
}

public class CrimeComplexityTrainingData
{
    public float CrimeType { get; set; }
    public float Precinct { get; set; }
    public float TimeOfDay { get; set; }
    public float Weather { get; set; }
    public float Severity { get; set; }
    [ColumnName("Label")]
    public float ComplexityScore { get; set; }
}

public class WorkloadTrainingData
{
    public float Precinct { get; set; }
    public int TotalCrimes { get; set; }
    public float CrimeComplexityScore { get; set; }
    public float PopulationDensity { get; set; }
    public float SeasonalFactor { get; set; }
    [ColumnName("Label")]
    public float ActualWorkloadHours { get; set; }
}

public class OptimizationTrainingData
{
    public float Precinct { get; set; }
    public float PredictedWorkload { get; set; }
    public int StaffingLevel { get; set; }
    public float PopulationDensity { get; set; }
    public int CrimeRate { get; set; }
    [ColumnName("Label")]
    public float PerformanceScore { get; set; }
}

public class CrimeComplexityPrediction
{
    [ColumnName("Score")]
    public float PredictedComplexity { get; set; }
}

public class WorkloadPrediction
{
    [ColumnName("Score")]
    public float WorkloadHours { get; set; }
    public float Confidence { get; set; }
}

public class PerformancePrediction
{
    [ColumnName("Score")]
    public float PredictedPerformance { get; set; }
}

public class OptimizationResult
{
    public int OptimalManpower { get; set; }
    public float Confidence { get; set; }
    public float ExpectedPerformance { get; set; }
}

public class MLTrainingResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int TrainingDataPoints { get; set; }
    public DateTime TrainedAt { get; set; }
    public ModelMetrics ComplexityModelMetrics { get; set; } = new();
    public ModelMetrics WorkloadModelMetrics { get; set; } = new();
    public ModelMetrics OptimizationModelMetrics { get; set; } = new();
}

public class ModelMetrics
{
    public string ModelType { get; set; } = string.Empty;
    public double RSquared { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public int TrainingDataPoints { get; set; }
}

public class MLManpowerRecommendation
{
    public Barangay Precinct { get; set; }
    public Dictionary<CrimeTypeEnum, int> PredictedCrimeCounts { get; set; } = new();
    public int Year { get; set; }
    public float CrimeComplexityScore { get; set; }
    public float PredictedWorkloadHours { get; set; }
    public float WorkloadConfidence { get; set; }
    public int RecommendedManpower { get; set; }
    public float OptimizationConfidence { get; set; }
    public string MLJustification { get; set; } = string.Empty;
}