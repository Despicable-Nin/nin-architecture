# AGENTS.md — Espasyo Backend (`nin-architecture/`)

**Audience:** AI coding agents. Human setup steps live in `README.md` (per-project, not duplicated here). This file governs **agent behavior, constraints, and required initial reads**.

---

## Solution shape (Clean Architecture — strict)

| Project | Layer | Depends on |
|---------|-------|------------|
| `espasyo.Domain` | Entities, enums, value objects, domain interfaces | nothing |
| `espasyo.Application` | Use cases, DTOs, repository/service interfaces, ML contracts (`IMachineLearningService`) | Domain only |
| `espasyo.Infrastructure` | EF Core (`ApplicationDbContext`, migrations), repositories, `MachineLearningService` (K-Means / SSA / anomalies / risk thresholds) | Application + Domain |
| `espasyo.WebAPI` | Controllers, Swagger, DI composition root, middleware | Application + Infrastructure |

Plus: `espasyo_console` (seeder), `nin-architecture.AppHost` + `nin-architecture.ServiceDefaults` (Aspire orchestration), and tests `Domain.Tests/`, `Infrastructure.Data.Repositories.Tests/`.

**Rule:** dependencies point inward only. Never invert them (e.g., Domain must not reference EF Core types).

## Commands (repo root unless noted)

| Task | Command |
|------|---------|
| Run API (port 5041, Swagger at `/swagger`) | `dotnet run --project espasyo.WebAPI` |
| Build solution | `dotnet build nin-architecture.sln` |
| Run all tests | `dotnet test nin-architecture.sln` |
| Add migration | `dotnet ef migrations add <name> --project espasyo.Infrastructure/espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI/espasyo.WebAPI.csproj --context <...>.Data.ApplicationDbContext --configuration Debug` |
| Apply migrations | `dotnet ef database update --project espasyo.Infrastructure/... --startup-project espasyo.WebAPI/... --context <...>.Data.ApplicationDbContext` |
| Run seeder (interactive) | `dotnet run --project espasyo_console` (waits 60s for API) |
| Run seeder (non-interactive) | `dotnet run --project espasyo_console -- -n` |
| Custom seeder wait | `dotnet run --project espasyo_console -- --wait=30` |

Notes:
- Migrations are managed from `espasyo.Infrastructure` but applied via the **WebAPI startup project** — keep that shape when automating.
- SQL Server is an **external Docker container** (port 1433); Aspire orchestrates processes only and must not provision SQL.
- API start-up applies migrations and seeds the admin user (`admin@example.com` / `Admin@123` per README).

## Environment & secrets

- Connection string in `espasyo.WebAPI/appsettings.json` (`ConnectionStrings:DefaultConnection`) — do not commit real secrets; leave the file as-is unless asked.
- `NEXTAUTH_SECRET` must match the frontend's value (shared JWT signing/validation). **Never commit it.**

## API surface (controllers)

`espasyo.WebAPI/Controllers/` — `IncidentController`, `AnalysisRunController`, `ForecastRunController`, `ForecastPreferencesController`, `ManpowerController`, `PrecinctsController`, `StreetController`, `UserController` (auth).

Frontend types mirror these DTOs by hand — **any DTO/endpoint change requires updating `espasyo/nextjs-auth-app/src/types/` the same day** (see root `AGENTS.md`).

## Constraints (enforced)

1. **Controllers are thin.** HTTP plumbing, model binding, and `IActionResult` mapping only. Business logic belongs in `espasyo.Application` use cases; data access behind repository interfaces.
2. **Analytics logic lives in `MachineLearningService` (Infrastructure)** behind `IMachineLearningService` (Application). Do not implement clustering/forecasting in controllers, and do not push ML params into the frontend.
3. **Domain purity:** entities/enums/value objects only; no EF attributes, no HTTP types in `espasyo.Domain`.
4. **Aspire:** AppHost starts `espasyo.WebAPI` + `espasyo_console` in order; the console waits for the API. Keep orchestration concerns in AppHost/ServiceDefaults, not in the API.
5. **Stray files:** `identifier.sqlite`, `espasyo.db`, `espasyo_dev.db` at repo root are debris — leave them unless the user asks.
6. **Stale README path:** `README.md` references an old `D:\hobby\nin-architecture`; canonical workspace is `D:\loa\sir_regie`. Don't propagate the old path.
7. **Spec first.** Review existing code and generate its counterpart spec before modifying it; carry out new specs once reviewed. See root `AGENTS.md` §"Agent operating rules".

## Initial reads (before any code change)

1. `README.md` (run/test/migration commands, connection string, Aspire notes)
2. `architecture.md` (layer responsibilities + ML subsystem design)
3. `espasyo.WebAPI/Program.cs` (DI graph, middleware pipeline)
4. The relevant controller under `espasyo.WebAPI/Controllers/` for the task
5. `espasyo_console/Program.cs` when touching seeding (smart seeding targets 5000 incidents; respects rate limits)
6. `WARP.md`, `SQLITE_IMPLEMENTATION.md`, `forecast-plan.md`, `api-reference.md` for the specific subsystem
