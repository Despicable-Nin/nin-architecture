# Data Provenance & Reproducibility — Espasyo Sample Data

**Status:** Working record for GA-02 (Establish sample-data provenance and reproducibility).  
**Reviewed by:** — (owner/adviser sign-off pending via GA-01 protocol).  
**Scope:** Backend sample-data seeder (`espasyo_console/`) and the checked-in SQLite sample.

---

## 1. Datasets covered

| Dataset | Location | Provenance | Suitability |
|---------|----------|------------|-------------|
| SQLite sample (checked-in) | `nin-architecture/identifier.sqlite` (stray file at repo root) | **Unverified.** Contents and origin have not been inspected (`PROJECT_GAP_ANALYSIS.md` review limitations: "No database contents were inspected"). | **Development / demo only.** Do **not** use as thesis evaluation evidence until contents are inspected and GA-01 approves it as the authoritative dataset. |
| Generated fixtures | `espasyo_console/` (IncidentGenerator, CoordinateGenerator, ManpowerSeeder, etc.) | **Synthetic.** Generated at runtime by calling the WebAPI (`http://localhost:5041/api/...`). Records, coordinates, timestamps, and manpower headcounts are randomized. | **Development / demo only.** Explicitly labeled synthetic. Not real-world trend evidence. |

## 2. Seeder behavior (generated fixtures)

- **Entry point:** `espasyo_console/Program.cs` — waits for the WebAPI, then seeds precincts → streets → incidents → manpower in that order.
- **Incident target:** 5,000 total incidents (`ContinueToTargetCount(..., 5000)`); smart-seeding skips if target already reached.
- **Date range:** 2020-12-31 to 2025-12-31 (5-year window, `IncidentGenerator.MinSeedDate`/`MaxSeedDate`).
- **Geography:** 9 barangays (Alabang, Ayala_Alabang, Sucat, Poblacion, Putatan, Tunasan, Cupang, Bayanan, Buli) from `JsonFiles/precincts.geojson`; coordinates are random points inside each barangay polygon (`CoordinateGenerator`).
- **Precinct selection:** Fetches real precincts from `/api/manpower/precincts` to keep precinct ↔ barangay alignment; maps precinct code → `Barangay` enum.
- **Manpower:** Random headcount 15–40 officers per precinct (`ManpowerSeeder`).
- **API dependency:** The seeder requires a running WebAPI on `http://localhost:5041`; it is **not** offline/self-contained.

## 3. Reproducibility (GA-02 acceptance criterion #2)

- All randomness is seeded from a single explicit `--seed` value (default `42`).
- Default `42` matches the `MLContext(seed: 42)` registrations in both infrastructure DI paths (`InfrastructureDependencyInjection.cs`, `SqliteInfrastructureDependencyInjection.cs`), so clustering is reproducible end-to-end.
- Usage:
  - `dotnet run --project espasyo_console -- --seed=42` (default seed)
  - `dotnet run --project espasyo_console -- --seed=12345` (custom seed)
- Same seed + same starting DB state ⇒ same incident count, case IDs, timestamps, coordinates, crime types, motives, severities, weather, addresses, and manpower headcounts across runs.
- Every seeded run logs `🔢 Fixture seed: <n> (all generated data is synthetic / development-only)` and `🎯 ... (seed: <n>, synthetic fixtures)`.

### Determinism details

| Component | RNG source | Seeded? |
|-----------|-----------|---------|
| Incident attributes (crime type, motive, severity, weather) | `EnumHelper.Random` | Yes (`EnumHelper.SetSeed`) |
| Incident coordinates | `CoordinateGenerator.Rng` (per call, seed + incident number) | Yes (`GetRandomPoint(barangay, seed)`) |
| Incident address (street + house number) | `AddressGenerator.Random` | Yes (`AddressGenerator.SetSeed`) |
| Incident timestamps & case IDs | Derived from seed + index (no `DateTime.Now`) | Yes |
| Precinct selection order | `IncidentGenerator.Rng` | Yes |
| Manpower headcounts | `ManpowerSeeder.Random` | Yes (`SeedMissingManpower(..., seed)`) |
| K-Means centroids / optimal-K | `MLContext(seed: 42)` | Yes (DI-registered, matches default seeder seed) |

## 4. Known limitations / sampling bias

- Generated incidents are **uniformly random** across crime types, motives, severities, weather, and barangays — they do **not** reflect real-world Muntinlupa crime distributions, seasonality, or spatial concentration.
- Timestamps are uniformly distributed across the 5-year window; no realistic temporal clustering (e.g., evening peaks, month-end effects).
- Coordinates are uniform random points inside barangay bounding boxes, constrained to the polygon — not real incident locations.
- Manpower headcounts are random 15–40 per precinct; not based on roster data or coverage requirements.
- Because data is synthetic, **forecasts, clustering, and allocation recommendations computed on it are not thesis evidence.** They are smoke-test fixtures only.

## 5. Labeling convention

Generated fixtures are **never** presented as real data:
- Console output prefixes every run with "synthetic fixtures".
- No `isSynthetic` DB column is added in GA-02 (labeling is operational/documentation-only; schema changes are out of scope).
- Thesis validation must use only the dataset and protocol approved in GA-01, never the random seeder output.

## 6. What remains (post-GA-02)

- Inspect the SQLite sample contents; record source, collection period, transformations, and limitations (GA-02 work item, pending database access).
- GA-01 owner/adviser approval of the evaluation protocol (defines what counts as thesis evaluation data).
- GA-03: make barangay geometry authoritative for ingestion/analysis/display.
- GA-04: correct clustering methodology and add reproducibility tests.
