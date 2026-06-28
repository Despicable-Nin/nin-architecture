# Unified Forecast Plan — Post-Prediction Filtering for Manpower Allocation

## Goal
Return a single, flat forecast response from `POST /incident/forecast/statistical` where every row is self-contained with all dimension labels. The UI can filter/slice by any field (precinct, crime type, time of day, cluster, risk level, trend, etc.) without additional API calls.

## Architecture

```
POST /incident/forecast/statistical
  └─ Handler orchestrates 3 independent services:
      ├─ TemporalForecastService   (monthly trends — Linear/SSA/Seasonal, split by timeOfDay, crimeType)
      ├─ SpatialForecastService    (distribute precinct forecast to clusters — proportional by history)
      └─ SeasonalForecastService   (decompose trend/seasonal/residual from historical data)
  └─ Merges all into one flat forecasts[] + top-level analytical fields
```

- Services are **independent** — no ordering dependency.
- Each lives in `espasyo.Application/Interfaces/` (contract) and `espasyo.Infrastructure/MachineLearning/` (implementation).
- The existing handler maps request → runs selected services → flattens → returns.

---

## Full Request Model

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
  "horizonUnit": "month",
  "confidenceLevel": 0.95,
  "modelType": "Linear",
  "includeSeasonality": true,
  "weightRecentData": true,
  "includeTimeOfDay": false,
  "includeDayOfWeek": false,
  "includeMonthOfYear": false,
  "includeTrend": true,
  "crimeTypeFilter": null,
  "severityFilter": null,
  "customThresholds": {
    "lowMax": 0.8,
    "mediumMax": 1.2,
    "highMax": 1.5,
    "trendIncreaseThreshold": 1.1,
    "trendDecreaseThreshold": 0.9
  },
  "predictionTypes": ["temporal", "spatial", "seasonal"]
}
```

### Request Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `clusterData` | `ClusterGroup[]` | required | Input clusters from K-Means |
| `horizon` | int | 6 | Number of periods to forecast |
| `horizonUnit` | string | "month" | "month", "week", or "day" |
| `confidenceLevel` | double | 0.95 | Prediction interval confidence |
| `modelType` | string | "Linear" | "Linear", "SSA", "Seasonal" |
| `includeSeasonality` | bool | true | Enable seasonal decomposition |
| `weightRecentData` | bool | true | Favor recent months in trend |
| `includeTimeOfDay` | bool | false | Split forecast into morning/afternoon/night |
| `includeDayOfWeek` | bool | false | Reserved for future weekly granularity |
| `includeMonthOfYear` | bool | false | Reserved for future monthly factors |
| `includeTrend` | bool | true | Include trend component |
| `crimeTypeFilter` | string[] | null | Only include these crime types in output |
| `severityFilter` | string[] | null | Only include these severities |
| `customThresholds` | DynamicThresholds | null | Override risk/trend thresholds |
| `predictionTypes` | string[] | ["temporal"] | Which services to run |

`predictionTypes` values: `"temporal"`, `"spatial"`, `"seasonal"`. Default `["temporal"]` = current behavior.

---

## Full Response Model

```json
{
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
      "timeOfDay": "morning"
    },
    {
      "id": "g1e9b2c3-...",
      "predictionType": "spatial",
      "precinct": 3,
      "crimeType": null,
      "clusterId": 2,
      "latitude": 14.55,
      "longitude": 121.02,
      "timestamp": "2026-01-01T00:00:00Z",
      "forecast": 1.8,
      "lowerBound": 1.2,
      "upperBound": 2.4,
      "confidence": 0.76,
      "trend": "increasing",
      "riskLevel": "medium",
      "timeOfDay": null
    }
  ],
  "temporalPatterns": {
    "peakTimeOfDay": "afternoon",
    "peakMonth": 12,
    "troughMonth": 6,
    "weekendEffect": 1.15
  },
  "decomposition": [
    {
      "precinct": 3,
      "crimeType": 5,
      "trend": [3.2, 3.4, 3.5, 3.7, 3.9, 4.1],
      "seasonal": [0.85, 0.92, 1.05, 1.12, 1.08, 1.02, 0.95, 0.88, 0.82, 0.90, 1.10, 1.15],
      "residual": [1.02, 0.98, 1.01, 0.99, ...],
      "strength": { "trend": 0.72, "seasonal": 0.88 },
      "peakMonth": 12,
      "troughMonth": 8
    }
  ],
  "holidayEffects": {
    "Christmas": 1.4,
    "NewYear": 1.3,
    "HolyWeek": 0.7,
    "AllSaints": 1.1
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
      "Coordinate with specialized units for enhanced response",
      "Implement proactive crime prevention measures"
    ]
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

### Response Fields

| Field | Type | Description |
|---|---|---|
| `forecasts` | `ForecastRow[]` | Flat, self-describing forecast rows — UI filters on any field |
| `temporalPatterns` | object | Top-level temporal summary (not per-row) |
| `decomposition` | `DecompositionRow[]` | Seasonal decomposition per (precinct, crimeType) |
| `holidayEffects` | object | PH holiday multipliers |
| `summary` | `ForecastSummary` | Existing summary logic |
| `dynamicThresholds` | `ThresholdCalculationResult` | Thresholds actually used |
| `generatedAt` | DateTime | Generation timestamp |
| `modelUsed` | string | Model type |

### ForecastRow (flat item)

| Field | Type | Nullable | Description |
|---|---|---|---|
| `id` | string | no | UUID for React key / selection |
| `predictionType` | string | no | "temporal", "spatial", or "seasonal" |
| `precinct` | int | no | Precinct ID |
| `crimeType` | int | yes | null for spatial rows |
| `clusterId` | uint | yes | null for temporal rows |
| `latitude` | double | yes | null for temporal rows |
| `longitude` | double | yes | null for temporal rows |
| `timestamp` | DateTime | no | Forecast period |
| `forecast` | double | no | Predicted count |
| `lowerBound` | double | no | Lower confidence bound |
| `upperBound` | double | no | Upper confidence bound |
| `confidence` | double | no | Confidence level (decays with horizon) |
| `trend` | string | no | "increasing", "decreasing", "stable" |
| `riskLevel` | string | no | "low", "medium", "high", "critical" |
| `timeOfDay` | string | yes | "morning", "afternoon", "night", or null |

### UI Filtering Use Cases

| Filter | Field | Works for |
|---|---|---|
| By precinct | `precinct` | temporal + spatial |
| By crime type | `crimeType` | temporal only (spatial rows are null) |
| By time of day | `timeOfDay` | temporal only (when `includeTimeOfDay: true`) |
| By risk level | `riskLevel` | temporal + spatial |
| By trend | `trend` | temporal + spatial |
| By cluster | `clusterId` | spatial only |
| By prediction type | `predictionType` | temporal / spatial / seasonal |
| By date range | `timestamp` | temporal + spatial |
| By forecast value | `forecast` (range) | temporal + spatial |
| By confidence | `confidence` (range) | temporal + spatial |

---

## Per-Service Plan

### 1. TemporalForecastService

**Input:** `ClusterGroup[]`, `ForecastParameters`  
**Output:** `List<ForecastRow>` where `predictionType = "temporal"`

**Logic:**
1. Group input by precinct (current logic)
2. Run per-precinct forecast (Linear/SSA/Seasonal)
3. If `includeTimeOfDay: true`, split each month's forecast:
   - Compute historical proportion per timeOfDay per (precinct, crimeType)
   - Produce 3 rows per month: morning/afternoon/night, values sum to total
4. Crime type breakdown uses weighted distribution:
   - Factor in: historical proportion, precinct area size, high-risk crime count
   - Formula: `weight = historicalRatio × precinctSizeFactor × riskLevelFactor`
5. Return flat rows

**timeOfDay split:** proportional by historical distribution, not independent models.

### 2. SpatialForecastService

**Input:** `ClusterGroup[]`, `ForecastParameters`  
**Output:** `List<ForecastRow>` where `predictionType = "spatial"`

**Logic:**
1. Run forecast at precinct level (same as temporal, or reuse its result)
2. For each month, distribute forecast across clusters:
   - `clusterForecast = precinctForecast × (clusterHistoricalCount / precinctHistoricalCount)`
3. Set `latitude`/`longitude` to cluster centroid
4. Crime type is **filter only** — not an input to spatial computation

### 3. SeasonalForecastService

**Input:** `ClusterGroup[]`, `ForecastParameters`  
**Output:** `DecompositionRow[]` (top-level, not forecast rows)

**Logic (moving-average decomposition, no external lib):**
1. Compute centered 12-month moving average → trend component
2. Detrend: `actual / trend` → seasonal + residual
3. Average by month (1-12) → seasonal indices
4. Residual = `actual / (trend × seasonal)`
5. Strength: `1 - variance(residual) / variance(detrended)`

**Holiday effects:** hardcoded PH lookup table with monthly multipliers (TBD: applied to forecast or just reported).

---

## Service Boundaries

| Service | Groups by | Output | Crime type role | timeOfDay role |
|---|---|---|---|---|
| Temporal | precinct | 1+ rows per precinct per month | Produces per-crimeType values (weighted) | Splits month into 3 rows |
| Spatial | cluster | 1 row per cluster per month | Filter only — no effect on computation | Not applicable |
| Seasonal | precinct | decomposition[] array (not forecast rows) | Per-precinct aggregate | Not applicable |

---

## Migration Path

| Phase | What | Changes |
|---|---|---|
| 1 | Refactor into `TemporalForecastService` | Move current `GenerateStatisticalForecast` logic into new service class, no behavioral change |
| 2 | Flat response + `predictionTypes` | Change response from `ForecastSeries[]` to `ForecastRow[]`, add `predictionTypes` to request |
| 3 | Time-of-day split | Implement proportional split by historical timeOfDay ratio when `includeTimeOfDay: true` |
| 4 | Crime type weighted breakdown | Implement per-crimeType distribution with precinct size + risk level factors |
| 5 | `SpatialForecastService` | Proportional cluster distribution from precinct total |
| 6 | `SeasonalForecastService` | Moving-average decomposition, return decomposition[] |
| 7 | Holiday effects | Hardcoded PH holiday lookup, apply/report multipliers |
| 8 | Cleanup | Remove old `ForecastSeries`/`ForecastPoint` models if no consumers remain |
