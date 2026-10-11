# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

```bash
# Build (CI builds Release with /p:TreatWarningsAsErrors=true, so any warning fails the PR)
dotnet build

# Test: runs both test projects (RaceDay.Core.Tests + RaceDay.API.Tests)
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~PlanGeneratorTests"
dotnet test --filter "FullyQualifiedName~PlanGeneratorTests.GeneratePlan_SkipsSwimPhase"

# Test with output
dotnet test --logger "console;verbosity=detailed"

# Run the API (http://localhost:5208, Swagger at /swagger in Development)
dotnet run --project src/RaceDay.API

# Frontend (from src/RaceDay.Web.React/)
npm install
npm run dev          # Vite on :5173
npm run build        # tsc -b && vite build (type errors fail the build)
npm run lint         # eslint
npm run test:e2e     # Playwright screenshot tests (needs API + preview server running)

# Full stack in containers: web on :8080, API on :5208
docker compose up --build
```

Permitted bash operations are restricted to `dotnet test:*` and `dotnet build:*` per `.claude/settings.local.json`.

## Before Making Changes

1. Read the relevant source files. Never modify code you have not read.
2. Run `dotnet build` to confirm the baseline compiles.
3. Run `dotnet test` to confirm the baseline is green.

## After Making Changes

1. Run `dotnet build`. The target is zero warnings, and CI enforces it.
2. Run `dotnet test`. All tests must pass; fix failures before proceeding.
3. Do not commit unless explicitly asked.

CI (`.github/workflows/pr_check.yml`) also fails the PR if merged line coverage drops below **80 %**, and it runs `npx tsc --noEmit` on the frontend.

## Code Conventions

### C# (.NET)
- Target framework: .NET 10 (`net10.0`), C# `LangVersion` default.
- Use `record` types for immutable domain models (see `Models/`).
- Prefer expression-bodied members and pattern matching.
- No public mutable state on domain objects.
- Validation lives in `Utilities/Validation.cs`. Add new guards there, not inline.
- Exceptions extend `RaceDayException` in `Exceptions/`.
- Constants belong in `Constants/` (`NutritionConstants.cs`, `SchedulingConstraints.cs`, `PlannerHeuristics.cs` for scoring weights, `ValidationLimits.cs` for input ranges, `AdvancedNutritionConfig.cs`, `ProductEffectivenessProfiles.cs`).
- Keep `PlanGenerator.cs` and `NutritionPlanService.cs` as the algorithm boundary. Do not leak algorithm logic into the API layer.

### TypeScript / React
- Strict TypeScript (`tsconfig*.json`). Do not relax settings.
- Functional components with hooks only; no class components. Form state lives in `src/hooks/usePlannerForm.ts`.
- API calls go through `src/api.ts`. Do not call endpoints inline.
- Styles are plain CSS in `src/styles/` (`tokens.css` holds design tokens; all sheets are imported from `App.tsx`). Do not introduce a CSS-in-JS library.

### General
- SOLID principles and Clean Architecture layer boundaries must be respected.
- No cross-layer imports: Core has no reference to API or React.
- XML doc comments are not required unless the method is complex and non-obvious.

## Testing Requirements

- Core unit tests live in `tests/RaceDay.Core.Tests/` (xUnit + Shouldly). API integration tests (`WebApplicationFactory`) live in `tests/RaceDay.API.Tests/`.
- Every new algorithm change needs a test in `AlgorithmImprovementTests.cs`, `PlanGeneratorTests.cs`, or `RegressionTests.cs`. `RegressionTests.cs` runs a sport × duration × intensity × caffeine × product-set matrix through `PlanInvariants.cs`, an independent checker of the scheduling rules — keep it passing rather than loosening it.
- Test method naming: `MethodName_Scenario_ExpectedResult`.
- Do not delete or disable existing tests; fix them if a change breaks them.

## What Not To Do

- Do not add features beyond what is asked.
- Do not refactor surrounding code when fixing a bug.
- Do not introduce new NuGet packages without asking.
- Do not push to remote; do not force-push.
- Do not use `--no-verify` to skip hooks.

---

## Architecture

Three-tier Clean Architecture. Dependencies flow inward only.

```
RaceDay.Web.React  (UI)
       ↓  HTTP / JSON
RaceDay.API        (REST API, ASP.NET Core Minimal API)
       ↓  DI / interfaces
RaceDay.Core       (business logic, domain model, no framework dependencies)
```

### RaceDay.Core

| Path | Responsibility |
|------|----------------|
| `Constants/` | Immutable numeric thresholds (do not hard-code numbers elsewhere) |
| `Models/` | Immutable `record` types for domain objects |
| `Services/` | Algorithm and calculation services, plus domain rules used by the API (`SportProductRules`, `TemperatureClassifier`) |
| `Repositories/` | Product and activity catalogue access |
| `Utilities/` | Cross-cutting validation and extension helpers |
| `Exceptions/` | Domain exception hierarchy |
| `Data/` | Embedded JSON catalogues (`maurten-products.json`, `sis-products.json`, `activities.json`) |

### RaceDay.API
Thin hosting layer. Endpoints are in `ApiEndpointExtensions.cs`, grouped under `/api/products`, `/api/activities`, `/api/plan` (`POST /generate`, rate limited) and `/api/metadata`, plus `/health`.
- `Contracts/`: request/response DTOs and `ContractMapper`. Core records are never serialized directly, so a Core rename does not change the JSON contract.
- `Infrastructure/`: `GlobalExceptionHandler` maps exceptions to ProblemDetails (`ValidationException` → 400 with `errors`, other `RaceDayException` → 400, `CatalogueLoadException` → 503, anything else → generic 500). Endpoints have no try/catch. `HostingExtensions` holds DI, CORS (`Cors:AllowedOrigins`), rate limiting (`RateLimiting:*`), Railway `PORT` binding and the JSON options (enums by name only; integers rejected).
- `Metadata/`: UI/config metadata services (presentation text lives here, not in Core); they read Core constants.

### RaceDay.Web.React
Vite + React 19 + TypeScript SPA. `src/api.ts` is the single API client, and `src/types.ts` mirrors the API DTOs.

**API URL wiring:** `api.ts` prefixes requests with `VITE_API_URL`, defaulting to `''` (same-origin `/api`). With it unset, dev goes through the Vite proxy (→ `localhost:5208`) and Docker goes through nginx. The Vercel build sets it from the `RAILWAY_API_URL` secret in `deploy.yml`. All errors arrive as `application/problem+json` and are parsed by the shared `request()` helper. The results view renders the plan as generated (`plan.race`, `plan.targets`, `plan.caffeineEnabled`), never the live form.

---

## Domain Model

| Type | File | Role |
|------|------|------|
| `AthleteProfile` | `Models/AthleteProfile.cs` | Weight, preferences, caffeine sensitivity |
| `ActivityInfo` | `Models/ActivityInfo.cs` | Sport type, duration, intensity, temperature |
| `RaceProfile` | `Models/RaceProfile.cs` | Combines athlete + activity + selected products |
| `TriathlonLegModel` | `Models/TriathlonLegModel.cs` | Optional swim/bike/run leg split driving the phase timeline |
| `NutritionTargets` | `Models/NutritionTargets.cs` | Calculated per-hour carb/fluid/sodium targets |
| `MultiNutrientTargets` | `Models/MultiNutrientTargets.cs` | Extended targets including caffeine ceiling |
| `NutritionEvent` | `Models/NutritionEvent.cs` | A single scheduled intake (time, product, amount) |
| `PlanResult` | `Models/PlanResult.cs` | Final output: list of events + warnings/errors |
| `Product` / `ProductEnhanced` | `Models/` | Catalogue item / algorithm view of it (with `ProductTexture`) |
| `IntensityLevel` | `Models/IntensityLevel.cs` | Easy / Moderate / Hard |
| `RacePhase` | `Models/RacePhase.cs` | Swim / Bike / Run / Transition |
| `SportType` | `Models/SportType.cs` | Run / Bike / Triathlon |
| `EventKind` | `Models/EventKind.cs` | PreRace / Sip / Intake (sips are exempt from spacing and hourly limits) |

---

## Algorithm Overview (`PlanGenerator.cs`)

Entry point: `NutritionPlanService` validates the inputs (`Validation.cs`; at most 50 products) and drops recovery products. It converts `Product` → `ProductEnhanced`: texture comes from `ProductType`, gels ≤ 22 g become LightGel, and unknown types throw `ValidationException`. It then calls `PlanGenerator.GeneratePlanWithDiagnostics`.

Two collaborators carry the rules, and every phase must use them:
- **`NutritionEventFactory`** builds every `NutritionEvent`, so kind, sip volume, portions and caffeine fields stay consistent. Never construct or `with`-copy events by hand.
- **`PlacementPolicy.CanPlace`** is the single legality check before any intake is added or moved. It covers race bounds, swim/T2 blackouts, solids before T2 and the finish, pairwise spacing, max 4 intakes per hour, caffeine (disabled flag, 40 % start, 45 min spacing, ceiling) and max same product in a row.

Supporting types: `PlanningContext` (per-call state), `ProductScorer`, `PlanValidator`.

`GeneratePlan` pipeline, in order: `AddPreRaceIntake` → `PlaceIntakesPaced` → `RebuildSipBackbone` → `FillDeficit` → `TrimOvershoot` → `EnsureCoverage` → `EnforceFrontLoadConstraint` (moves events out of the front window rather than deleting them). Then `Finalize` computes cumulative totals once, and `PlanValidator.Validate` runs once. `PlanResult.Targets` carries the targets, so the API does not recompute them.

Plans aim at the carb target (within tolerance). Some product sets can't reach it within the spacing rules (e.g. chews only); those plans carry an "underdelivers" warning instead of breaking the rules.

### Key Numeric Constants

| Constraint | Value |
|------------|-------|
| Carbs Easy / Moderate / Hard (g/h) | 50 / 70 / 90 (+10 for races > 5 h) |
| Fluid base (ml/h) | 500 (+200 hot, −100 cold; min 300, max 900) |
| Sodium base (mg/h) | 400 (+200 hot; min 300, max 1000) |
| Min gel spacing (bike / run) | 15 / 20 min |
| Min solid spacing (bike / run) | 25 / 30 min |
| Min drink spacing | 12 min |
| Min caffeine spacing | 45 min |
| Max intakes per hour | 4 |
| Caffeine ceiling Easy / Moderate / Hard | 1 / 3 / 4 mg/kg |
| Caffeine windows | 40–55 %, 65–80 %, 85–95 % of duration |
| Triathlon bike / run carb split | 70 % / 30 % |
| Sip interval / volume | 10 min / 50 ml |
| Min coverage ratio | 85 % |
| Max front-load fraction | 40 % in first 25 % of time |

---

## File Quick-Reference

| Task | File |
|------|------|
| Algorithm logic | `src/RaceDay.Core/Services/PlanGenerator.cs`, `PlacementPolicy.cs`, `NutritionEventFactory.cs` |
| Per-hour nutrition targets | `src/RaceDay.Core/Services/NutritionCalculator.cs`, `Constants/NutritionConstants.cs` |
| Timing / spacing rules | `src/RaceDay.Core/Constants/SchedulingConstraints.cs` |
| Advanced config | `src/RaceDay.Core/Constants/AdvancedNutritionConfig.cs` |
| API endpoints | `src/RaceDay.API/ApiEndpointExtensions.cs` |
| DI / startup | `src/RaceDay.API/Program.cs`, `Infrastructure/HostingExtensions.cs` |
| API contract (DTOs) | `src/RaceDay.API/Contracts/` |
| API client (frontend) | `src/RaceDay.Web.React/src/api.ts` |
| React components | `src/RaceDay.Web.React/src/components/` |
| Unit tests | `tests/RaceDay.Core.Tests/` |
| API integration tests | `tests/RaceDay.API.Tests/` |
| CI pipeline | `.github/workflows/pr_check.yml` |
| Claude Code permissions | `.claude/settings.local.json` |

Further background lives in `README.md`, `TESTING.md` and `ALGORITHM_IMPROVEMENTS.md`. `AGENT.md` just points back to this file.
