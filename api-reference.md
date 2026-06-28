# Forecast API

## `POST /api/incident/forecast/statistical`

Single endpoint. `predictionTypes` selects which services run.

### Request

```json
{
  "clusterData": [
    {
      "clusterId": 1,
      "clusterItems": [
        {
          "caseId": "C001",
          "latitude": 14.55,
          "longitude": 121.02,
          "month": 1,
          "day": 15,
          "year": 2025,
          "timeOfDay": "morning",
          "precinct": 3,
          "crimeType": 5,
          "clusterId": 1
        }
      ]
    }
  ],
  "horizon": 6,
  "confidenceLevel": 0.95,
  "modelType": "Linear",
  "includeSeasonality": true,
  "weightRecentData": true,
  "includeTimeOfDay": false,
  "includeMonthOfYear": false,
  "includeTrend": true,
  "crimeTypeFilter": null,
  "severityFilter": null,
  "customThresholds": null,
  "predictionTypes": ["temporal", "spatial", "seasonal"]
}
```

### Response

```json
{
  "series": [],
  "forecasts": [
    {
      "id": "f0e8a1b2-...",
      "predictionType": "temporal",
      "precinct": 3,
      "crimeType": 5,
      "clusterId": null,
      "latitude": null,
      "longitude": null,
      "timestamp": "2026-01-01T00:00:00Z",
      "forecast": 4.2,
      "lowerBound": 3.1,
      "upperBound": 5.3,
      "confidence": 0.76,
      "trend": "increasing",
      "riskLevel": "high",
      "timeOfDay": null
    }
  ],
  "spatial": [
    {
      "id": "g1e9b2c3-...",
      "precinct": 3,
      "clusterId": 2,
      "latitude": 14.55,
      "longitude": 121.02,
      "timestamp": "2026-01-01T00:00:00Z",
      "forecast": 1.8,
      "lowerBound": 1.2,
      "upperBound": 2.4,
      "confidence": 0.76,
      "trend": "increasing",
      "riskLevel": "medium"
    }
  ],
  "decomposition": [
    {
      "precinct": 3,
      "crimeType": 5,
      "trend": [3.2, 3.4, 3.5, 3.7, 3.9, 4.1],
      "seasonal": [0.85, 0.92, 1.05, 1.12, 1.08, 1.02, 0.95, 0.88, 0.82, 0.90, 1.10, 1.15],
      "residual": [1.02, 0.98, 1.01, 0.99],
      "strength": { "trend": 0, "seasonal": 0.88 },
      "peakMonth": 12,
      "troughMonth": 8
    }
  ],
  "metrics": {
    "meanAbsoluteError": 0.43,
    "rootMeanSquareError": 0.61,
    "meanAbsolutePercentageError": 12.5,
    "modelAccuracy": 0.87
  },
  "summary": {
    "totalForecasts": 54,
    "highRiskPredictions": 9,
    "criticalRiskPredictions": 3,
    "overallTrend": "decreasing",
    "dominantRiskLevel": "critical",
    "averageConfidence": 0.59,
    "keyInsight": "Positive trend: Crime rates show declining pattern...",
    "recommendedActions": [
      "Deploy additional patrol units during high-risk periods",
      "Coordinate with specialized units for enhanced response"
    ]
  },
  "explanation": {
    "modelDescription": "Linear forecasting analyzes recent trends...",
    "confidenceExplanation": "Confidence levels indicate prediction reliability...",
    "trendAnalysis": "Trends show directional changes..."
  },
  "dynamicThresholds": {
    "globalThresholds": { "lowMax": 0.8, "mediumMax": 1.2, "highMax": 1.5 },
    "precinctSpecificThresholds": {},
    "totalDataPointsUsed": 0,
    "calculationMethod": "user-provided",
    "warnings": []
  },
  "generatedAt": "2026-06-28T06:27:37Z",
  "modelUsed": "Linear"
}
```
