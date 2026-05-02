# Backend Architecture (nin-architecture)

## Overview
The backend for the Espasyo application is built using ASP.NET Core (.NET 8) and strictly follows **Clean Architecture** principles. It utilizes **.NET Aspire** for local orchestration and development hosting.

## Clean Architecture Layers

### 1. Domain Layer (`espasyo.Domain`)
- **Responsibility**: Contains enterprise logic and types.
- **Contents**: Entities (e.g., `Incident`, `User`, `Street`), Value Objects, Enums, and Domain Interfaces.
- **Dependencies**: None. This is the core of the system and does not depend on any other project.

### 2. Application Layer (`espasyo.Application`)
- **Responsibility**: Contains business logic and use cases.
- **Contents**: CQRS commands/queries (if used), DTOs, interfaces for infrastructure services (e.g., repository interfaces), and application-specific exceptions.
- **Dependencies**: Depends ONLY on the Domain layer.

### 3. Infrastructure Layer (`espasyo.Infrastructure`)
- **Responsibility**: Implements interfaces defined in the Application layer and handles external concerns.
- **Contents**: 
  - Data access implementation using **Entity Framework Core**.
  - `ApplicationDbContext` and Code-First Migrations.
  - Repositories implementing application interfaces.
  - External service integrations (e.g., authentication tokens, external APIs).
- **Dependencies**: Depends on the Application and Domain layers.

### 4. Presentation Layer (`espasyo.WebAPI`)
- **Responsibility**: Exposes the system to the outside world.
- **Contents**: Controllers/Minimal APIs, Swagger configuration, middleware, and dependency injection composition root.
- **Dependencies**: Depends on Application and Infrastructure layers.

## Additional Components

### Data Seeding (`espasyo_console`)
- A console application dedicated to seeding initial data.
- Capable of rate-limited interactions with GIS geocoding services to populate incident latitude/longitude data.

### .NET Aspire Orchestration (`nin-architecture.AppHost` & `ServiceDefaults`)
- **AppHost**: Orchestrates the startup of the `espasyo.WebAPI` and `espasyo_console` projects, ensuring correct startup order (Console waits for API).
- **Database Connection**: Aspire does not provision the SQL Server container; instead, the Web API connects to an externally managed SQL Server Docker container via the connection string in `appsettings.json`.

## Database
- **Provider**: SQL Server (running in Docker locally).
- **ORM**: Entity Framework Core.
- **Migrations**: Code-First approach managed from the `espasyo.Infrastructure` project but applied to the `espasyo.WebAPI` startup project.

---

## Machine Learning & Analytics Subsystem (ML.NET)

### 1. K-Means Clustering
- **Interface**: `IMachineLearningService` (Application layer)
- **Implementation**: `MachineLearningService` (Infrastructure layer)
- **Default Features**: `Latitude`, `Longitude`, plus **one user-selected demographic feature** (e.g., `CrimeType`, `Severity`, `Weather`). This is intentional — defaulting to all demographics causes the curse of dimensionality and dilutes geographic proximity.
- **Feature Engineering**:
  - Categorical features (CrimeType, Severity, Weather, Motive, PoliceDistrict) → **One-Hot Encoding**.
  - Float coordinates (Latitude, Longitude) → **Type Conversion to Single**.
  - Numeric features → **Min-Max Normalization**.
  - All encoded features are concatenated and then **Mean-Variance Normalized** before training.
- **Training**: The best model is selected across `N` runs by minimizing `AverageDistance` (intra-cluster distance).
- **Exposed Queries**: `GetClusters` (raw list) and `GetGroupedClusters` (structured by ClusterId, enriched with Month, Year, TimeOfDay, Precinct, CrimeType).

### 2. Statistical Forecasting
- **Interface**: `IMachineLearningService`
- **Primary Algorithm**: ML.NET **Singular Spectrum Analysis (SSA)** — well-suited for seasonal crime time series.
- **Data Preparation**: Cluster items are grouped by `(Precinct, CrimeType)` and aggregated into monthly counts before forecasting.
- **Sparse Data Handling**: If a Precinct × CrimeType group has fewer than 12 months of data, it is skipped. *TODO: a future improvement is to aggregate sparse groups up to the precinct level before applying SSA.*
- **Fallback Models**: If SSA fails, the service falls back to Linear Trend (OLS regression).
- **Risk Thresholds**: Dynamically calculated from historical precinct-level data ratios. Precinct-specific thresholds are used if enough data is available; otherwise, global thresholds apply.
- **Validation**: `ValidateForecastModel` uses a train/test split (hold-out last 6 months) and computes MAPE. MAPE < 25% is considered reliable.
- **Data Quality**: `AssessDataQuality` checks total data point count (≥ 100), temporal coverage (≥ 24 months), and outlier rate (< 10%).

### 3. Manpower Allocation Optimization
- **Service**: `MLManpowerAllocationService` (Application layer)
- **Architecture**: Three sequential ML regression models.
  1. **Crime Complexity Model** (LBFGS Poisson Regression): Learns a complexity score per incident from historical clustering patterns (crime type co-occurrence, seasonal factors, geographic factors).
  2. **Workload Prediction Model** (LBFGS Poisson Regression): Predicts total workload hours required given predicted crime counts, crime complexity score, population density, and seasonal factor.
  3. **Manpower Optimization Model** (SDCA Regression): Simulates different staffing levels and selects the one that maximizes predicted performance score (crime clearance rate proxy).
- **Model Persistence (Development Strategy)**:
  - On startup, the service attempts to **load pre-trained models from disk** (`MLModels/*.zip`).
  - If no `.zip` files exist (first-ever run in this environment), the service trains all three models on the first API request and **saves them to disk** automatically.
  - All subsequent requests — including after application restarts — load from disk instantly, with no retraining.
  - **To force a retrain** (e.g., after collecting significant new data): delete the `.zip` files from the `MLModels/` directory and restart the application.
  - *In production*: move training invocation to a scheduled `IHostedService` (e.g., weekly) to fully decouple it from request handling.
