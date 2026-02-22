# Claude Code Instructions

## Essential Commands

```bash
# Build
dotnet build

# Test (all 157 tests must pass before any commit)
dotnet test

# Test with output
dotnet test --logger "console;verbosity=detailed"

# Frontend (from src/RaceDay.Web.React/)
npm install
npm run dev
npm run build
```

Permitted bash operations are restricted to `dotnet test:*` and `dotnet build:*` per `.claude/settings.local.json`.

## Before Making Changes

1. Read the relevant source files — never modify code you have not read.
2. Run `dotnet build` to confirm the baseline compiles.
3. Run `dotnet test` to confirm the baseline is green (157 tests).

## After Making Changes

1. Run `dotnet build` — zero warnings is the target.
2. Run `dotnet test` — all 157 tests must pass; fix failures before proceeding.
3. Do not commit unless explicitly asked.

## Code Conventions

### C# (.NET)
- Target framework: .NET 8 / C# 12.
- Use `record` types for immutable domain models (see `Models/`).
- Prefer expression-bodied members and pattern matching.
- No public mutable state on domain objects.
- Validation lives in `Utilities/Validation.cs` — add new guards there, not inline.
- Exceptions extend `RaceDayException` in `Exceptions/`.
- Constants belong in `Constants/` (see `NutritionConstants.cs`, `SchedulingConstraints.cs`, `AdvancedNutritionConfig.cs`).
- Keep `PlanGenerator.cs` and `NutritionPlanService.cs` as the algorithm boundary — do not leak algorithm logic into the API layer.

### TypeScript / React
- Strict TypeScript (`tsconfig.json` — do not relax settings).
- Functional components with hooks only; no class components.
- API calls go through `src/api.ts` — do not call endpoints inline.
- Styles in `App.css`; do not introduce a CSS-in-JS library.

### General
- SOLID principles and Clean Architecture layer boundaries must be respected.
- No cross-layer imports: Core has no reference to API or React.
- XML doc comments are not required unless the method is complex and non-obvious.

## Testing Requirements

- Unit tests live in `tests/RaceDay.Core.Tests/`.
- Every new algorithm change needs a test in `AlgorithmImprovementTests.cs` or `AdvancedPlanGeneratorTests.cs`.
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
RaceDay.Core       (business logic, domain model — no framework dependencies)
```

### RaceDay.Core

| Path | Responsibility |
|------|----------------|
| `Constants/` | Immutable numeric thresholds (do not hard-code numbers elsewhere) |
| `Models/` | Immutable `record` types for domain objects |
| `Services/` | Algorithm and calculation services |
| `Repositories/` | Product and activity catalogue access |
| `Utilities/` | Cross-cutting validation and extension helpers |
| `Exceptions/` | Domain exception hierarchy |
| `Data/` | Embedded JSON product catalogues |

### RaceDay.API
Thin hosting layer. All endpoint definitions are in `ApiEndpointExtensions.cs`. `Program.cs` wires DI and middleware only.

### RaceDay.Web.React
Vite + React + TypeScript SPA. `src/api.ts` is the single API client; components are in `src/components/`.

---

## Domain Model

| Type | File | Role |
|------|------|------|
| `AthleteProfile` | `Models/AthleteProfile.cs` | Weight, preferences, caffeine sensitivity |
| `ActivityInfo` | `Models/ActivityInfo.cs` | Sport type, duration, intensity, temperature |
| `RaceProfile` | `Models/RaceProfile.cs` | Combines athlete + activity + selected products |
| `NutritionTargets` | `Models/NutritionTargets.cs` | Calculated per-hour carb/fluid/sodium targets |
| `MultiNutrientTargets` | `Models/MultiNutrientTargets.cs` | Extended targets including caffeine ceiling |
| `NutritionEvent` | `Models/NutritionEvent.cs` | A single scheduled intake (time, product, amount) |
| `PlanResult` | `Models/PlanResult.cs` | Final output: list of events + diagnostics |
| `Product` / `ProductEnhanced` | `Models/` | Catalogue items with macro data |
| `IntensityLevel` | `Models/IntensityLevel.cs` | Easy / Moderate / Hard / Max |
| `RacePhase` | `Models/RacePhase.cs` | Swim / Bike / Run / Transition |
| `SportType` | `Models/SportType.cs` | Triathlon, Cycling, Running, etc. |

---

## Algorithm Overview (`PlanGenerator.cs`)

Entry point: `NutritionPlanService` → `PlanGenerator`.

1. **Target calculation** — derive per-hour carb/fluid/sodium targets from intensity, weight, and temperature (`NutritionCalculator`, `NutritionConstants`).
2. **Multi-nutrient targets** — extend with intensity-based caffeine ceiling (mg/kg, `SchedulingConstraints`).
3. **Product scoring** — rank catalogue items against targets; drinks preferred first.
4. **Event scheduling** — place intake events respecting minimum spacing, phase transition blackouts, clustering prevention, and intake-per-hour limits.
5. **Caffeine placement** — strategic windows at 40–55 %, 65–80 %, 85–95 % of race duration.
6. **Sip scheduling** — drink events at 10-minute intervals, 50 ml per sip.
7. **Coverage check** — ≥ 85 % of race time must have fuelling coverage.
8. **Front-load guard** — first 25 % of race cannot exceed 40 % of total carbs.
9. **Validation** — 8-point validation before returning `PlanResult`.

### Key Numeric Constants

| Constraint | Value |
|------------|-------|
| Carbs Easy / Moderate / Hard (g/h) | 50 / 70 / 90 |
| Fluid base (ml/h) | 500 (+200 hot, −100 cold; min 300, max 900) |
| Sodium base (mg/h) | 400 (+200 hot; min 300, max 1000) |
| Min gel spacing (bike / run) | 15 / 20 min |
| Min solid spacing (bike / run) | 25 / 30 min |
| Min drink spacing | 12 min |
| Min caffeine spacing | 45 min |
| Caffeine ceiling Easy/Moderate/Hard/Max | 1/3/4/5 mg/kg |
| Sip interval / volume | 10 min / 50 ml |
| Min coverage ratio | 85 % |
| Max front-load fraction | 40 % in first 25 % of time |

---

## File Quick-Reference

| Task | File |
|------|------|
| Algorithm logic | `src/RaceDay.Core/Services/PlanGenerator.cs` |
| Per-hour nutrition targets | `src/RaceDay.Core/Constants/NutritionConstants.cs` |
| Timing / spacing rules | `src/RaceDay.Core/Constants/SchedulingConstraints.cs` |
| Advanced config | `src/RaceDay.Core/Constants/AdvancedNutritionConfig.cs` |
| API endpoints | `src/RaceDay.API/ApiEndpointExtensions.cs` |
| DI / startup | `src/RaceDay.API/Program.cs` |
| API client (frontend) | `src/RaceDay.Web.React/src/api.ts` |
| React components | `src/RaceDay.Web.React/src/components/` |
| Unit tests | `tests/RaceDay.Core.Tests/` |
| Claude Code permissions | `.claude/settings.local.json` |

---

## Tests

All 157 tests in `tests/RaceDay.Core.Tests/` must pass.

| File | Scope |
|------|-------|
| `NutritionCalculatorTests.cs` | Target calculation |
| `PlanGeneratorTests.cs` | Core scheduling |
| `ValidationTests.cs` | Input validation |
| `ActivityRepositoryTests.cs` | Catalogue access |
| `AlgorithmImprovementTests.cs` | Algorithm v2 regressions (7) |
| `AdvancedPlanGeneratorTests.cs` | Advanced scenarios (36) |
