# FlightTracker

[![Build](https://img.shields.io/github/actions/workflow/status/rick2909/FlightTracker/build.yml?branch=main&style=for-the-badge&label=build)](https://github.com/rick2909/FlightTracker/actions/workflows/build.yml)
[![Tests](https://img.shields.io/github/actions/workflow/status/rick2909/FlightTracker/test.yml?branch=main&style=for-the-badge&label=tests)](https://github.com/rick2909/FlightTracker/actions/workflows/test.yml)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Sass](https://img.shields.io/badge/Sass-CC6699?style=for-the-badge&logo=sass&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-5A2B81?style=for-the-badge&logo=xunit&logoColor=white)
[![Coverage](https://img.shields.io/codecov/c/github/rick2909/FlightTracker?branch=main&style=for-the-badge&logo=codecov)](https://app.codecov.io/gh/rick2909/FlightTracker)

A modern, multi-platform flight tracking application (web, desktop, mobile) built with .NET and Clean Architecture principles. The active presentation architecture is FlightTracker.Api + Blazor Server (FlightTracker.Web), with typed API clients used by the web UI.

## Status

Current status (March 2026):

- FlightTracker.Api is active with versioned v1 controllers, Swagger, PAT auth support, and rate limiting.
- FlightTracker.Web is active as a Blazor Server app and consumes the API through typed HttpClient clients.
- Core slices are implemented end-to-end: airports, flights, user flights, passport/stats, preferences, and PAT management.
- Infrastructure includes EF Core + Identity, SQLite migrations, deterministic seed data, and provider integrations (Aviationstack, timeapi.io, ADSBdb).
- Solution and project builds are green in Debug.

Current focus:

- Provider resilience and config hardening.
- Expand targeted tests (especially cancellation and branch-heavy service paths).
- Continue API contract refinement and client parity improvements.

## Architecture (Clean Architecture Inspired)

Layering (inward dependencies only):

```text
Presentation (API / Web / Desktop / Mobile)  -->  Application  -->  Domain
                       ^                               |
                       |                               v
                    Infrastructure (implements interfaces)
```

- **Domain**: Entities & (future) value objects / domain interfaces (pure, no EF attributes).
- **Application**: Use cases, service logic, DTOs, mapping, validation, interface abstractions for external systems.
- **Infrastructure**: EF Core, repository implementations, Identity, external API clients, persistence concerns.
- **Presentation**: API controllers / Blazor UI / MAUI host layers (not yet added here).

See `.github/copilot-instructions.md` for full rules.

## Project Structure (Current)

```text
FlightTracker/
├─ FlightTracker.Application/
│  ├─ Dtos/
│  ├─ Mapping/
│  ├─ Repositories/Interfaces/
│  ├─ Results/
│  └─ Services/
├─ FlightTracker.Domain/
│  ├─ Entities/
│  └─ Enums/
├─ FlightTracker.Infrastructure/
│  ├─ Data/
│  ├─ External/
│  ├─ Repositories/
│  └─ Time/
├─ FlightTracker.Api/
│  ├─ Contracts/
│  ├─ Controllers/V1/
│  └─ Infrastructure/
├─ FlightTracker.Web/
│  ├─ Api/Clients/
│  ├─ Components/
│  ├─ Models/
│  ├─ Pages/
│  ├─ Services/
│  ├─ Styling/
│  └─ wwwroot/
├─ Tests/
│  ├─ FlightTracker.Application.Tests/
│  ├─ FlightTracker.Domain.Tests/
│  ├─ FlightTracker.Infrastructure.Tests/
│  └─ FlightTracker.Web.Tests/
└─ doc/
```

## CI and Quality Notes

- Build badge targets GitHub Actions workflow file `build.yml` on `main`.
- Test badge targets GitHub Actions workflow file `test.yml` on `main`.
- Coverage badge targets Codecov for `rick2909/FlightTracker` on `main`.
- If your workflow file uses a different name, update the badge URL segments `build.yml` and `test.yml`.
- For private repositories, add `CODECOV_TOKEN` in GitHub Settings -> Secrets and variables -> Actions.
- To block merges when checks fail, set branch protection (or a ruleset) on `main` and mark `Build` and `Tests` checks as required.

## Key Principles

- No EF/Core attributes in Domain.
- All async public methods end with `Async` and accept `CancellationToken` last.
- DTO boundary between Application -> Presentation (no leaking EF/Identity models).
- External providers (OpenSky, FR24, Aviationstack) hidden behind interfaces.
- Logging & persistence in Infrastructure only.
- Repositories and low-level external clients return raw data/null/collections; Application services map outcomes to `Result`/`Result<T>`.

See `doc/Architecture.md` for the repository and Result return-contract policy.

## Technology (Current / Planned)

| Concern | Tech |
| ------- | ---- |
| Language | C# 13 / .NET 10 |
| Auth | ASP.NET Core Identity (int keys) |
| ORM | EF Core 10 |
| UI (current) | ASP.NET Core API + Blazor Server (default), Radzen, ApexCharts |
| Realtime (planned) | SignalR |
| Mapping | AutoMapper |
| External Flight Data (current/planned) | timeapi.io (current); Aviationstack (airport live departures/arrivals); ADSBdb (aircraft metadata); OpenSky/FR24 (planned) |
| Caching (planned) | MemoryCache / Redis (later) |

## External APIs and Data Providers

This project integrates with external services via Application-layer interfaces, implemented in Infrastructure. Current and planned providers:

- Current
  - timeapi.io (Time zone by coordinates)
    - Endpoint: `GET https://timeapi.io/api/timezone/coordinate?latitude={lat}&longitude={lon}`
    - Purpose: Resolve IANA time zone for departure/arrival airports when rendering user flights.
    - Integration: `ITimeApiService` (Application) with `TimeApiService` (Infrastructure). Registered via `AddHttpClient` with a short timeout for resiliency. Failures return `null` and the UI degrades gracefully.

- Planned (under evaluation)
  - OpenSky Network
    - Use cases: live positions, recent flights, basic schedule/window queries for known ICAO/IATA.
    - Notes: Public endpoints have limits; authenticated access recommended. Data completeness varies by region/altitude.
    - C# client: steveberdy/OpenSky (<https://github.com/steveberdy/OpenSky>) — may be used in Infrastructure behind our provider interface if it fits needs; Domain/Application remain SDK-free.
  - Flightradar24 (FR24)
    - Use cases: schedules, status, historical tracks. FR24 APIs are not officially documented; terms and access must be carefully reviewed before use.
  - Aviationstack
    - Use cases: airport departures/arrivals, flight status, basic enrichment (airline/aircraft). API key required; rate limits apply.
    - Current integration: `IAirportLiveService` implemented by `AviationstackService` and used by Airports page (toggle for live data).
  - ADSBdb
    - Use cases: aircraft registry lookup and enrichment (registration/tail, ICAO24/hex, type/model/manufacturer; possibly age/photos when available).
    - Notes: Useful to enrich flights and user aircraft with reliable metadata. Check API key requirements, rate limits, and ToS before use.
    - Current integration: `IFlightRouteLookupClient` implementation is registered and used for route/metadata lookup.

### Integration approach'

- Abstractions live inward (Application): e.g., `ITimeApiService`, future `IFlightDataProvider` (name TBD).
- Implementations live outward (Infrastructure), injected via DI.
- No external SDKs referenced by Domain or Application.
- Responses are mapped to Application DTOs before crossing to Presentation.

### Configuration & resiliency

- Timeouts kept short (few seconds) to avoid blocking pages; timeouts/errors return `null` and callers proceed without enrichment.
- Consider caching (memory) for stable lookups like airport time zones and static references.
- Provider credentials are read from configuration and user secrets; never commit keys. For Aviationstack, set `Aviationstack:ApiKey` in appsettings.Development.json or via user secrets on the Web project.
- Add retries/backoff only where providers recommend it; respect rate limits and terms.

### Legal/usage notes

- Verify provider terms (OpenSky/FR24/Aviationstack) before enabling in production.
- Attribute sources where required and avoid retaining PII.

## Seed Data

`SeedData` provides deterministic airports, sample flights, and optional users.

Development or test usage (in-memory DB):

```csharp
var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
    .UseInMemoryDatabase("FlightTrackerDev")
    .Options;
using var ctx = new FlightTrackerDbContext(options);
await SeedData.SeedAsync(ctx);
```

For integration tests with relational semantics use SQLite in-memory:

```csharp
var conn = new SqliteConnection("DataSource=:memory:");
await conn.OpenAsync();
var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
    .UseSqlite(conn)
    .Options;
using var ctx = new FlightTrackerDbContext(options);
await ctx.Database.EnsureCreatedAsync();
await SeedData.SeedAsync(ctx);
```

## Build and Test

Prerequisites: .NET 10 SDK, Node.js (for frontend assets), optional SQLite tools.

Build the full solution:

```powershell
dotnet build FlightTracker.sln
```

Build only the web project:

```powershell
dotnet build .\FlightTracker.Web\FlightTracker.Web.csproj -c Debug
```

Run all tests:

```powershell
dotnet test FlightTracker.sln
```

Run tests with coverage (Coverlet collector):

```powershell
dotnet test FlightTracker.sln --collect:"XPlat Code Coverage"
```

Frontend asset commands:

```powershell
npm run build
npm run watch
```

## Database Migrations (Root SQLite)

FlightTracker uses the solution-root SQLite file:

- Path: `./flighttracker.dev.db`
- API runtime (cwd = `FlightTracker.Api`) points to `../flighttracker.dev.db`

Run migrations explicitly against the root DB:

```powershell
dotnet ef database update \
  --project .\FlightTracker.Infrastructure\FlightTracker.Infrastructure.csproj \
  --startup-project .\FlightTracker.Api\FlightTracker.Api.csproj \
  --context FlightTrackerDbContext \
  --connection "Data Source=$((Resolve-Path .\flighttracker.dev.db).Path)"
```

Create a new migration in Infrastructure:

```powershell
dotnet ef migrations add <MigrationName> \
  --project .\FlightTracker.Infrastructure\FlightTracker.Infrastructure.csproj \
  --startup-project .\FlightTracker.Api\FlightTracker.Api.csproj \
  --context FlightTrackerDbContext \
  --output-dir Data\Migrations
```

## Merge Old API-local DB Into Root DB

If you have data in `FlightTracker.Api/flighttracker.dev.db` and want to merge into `./flighttracker.dev.db`:

1. Back up both files first.
2. Use SQLite attach + table copy (examples below).
3. Keep root DB as source of truth going forward.

Example with sqlite3:

```powershell
sqlite3 .\flighttracker.dev.db "ATTACH DATABASE './FlightTracker.Api/flighttracker.dev.db' AS old; \
PRAGMA foreign_keys=OFF; \
INSERT OR IGNORE INTO Airports SELECT * FROM old.Airports; \
INSERT OR IGNORE INTO Airlines SELECT * FROM old.Airlines; \
INSERT OR IGNORE INTO Aircraft SELECT * FROM old.Aircraft; \
INSERT OR IGNORE INTO Flights SELECT * FROM old.Flights; \
INSERT OR IGNORE INTO AspNetUsers SELECT * FROM old.AspNetUsers; \
INSERT OR IGNORE INTO AspNetRoles SELECT * FROM old.AspNetRoles; \
INSERT OR IGNORE INTO AspNetUserRoles SELECT * FROM old.AspNetUserRoles; \
INSERT OR IGNORE INTO AspNetUserClaims SELECT * FROM old.AspNetUserClaims; \
INSERT OR IGNORE INTO AspNetRoleClaims SELECT * FROM old.AspNetRoleClaims; \
INSERT OR IGNORE INTO AspNetUserLogins SELECT * FROM old.AspNetUserLogins; \
INSERT OR IGNORE INTO AspNetUserTokens SELECT * FROM old.AspNetUserTokens; \
INSERT OR IGNORE INTO UserPreferences SELECT * FROM old.UserPreferences; \
INSERT OR IGNORE INTO UserFlights SELECT * FROM old.UserFlights; \
INSERT OR IGNORE INTO PersonalAccessTokens SELECT * FROM old.PersonalAccessTokens; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM Airports) WHERE name='Airports'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM Airlines) WHERE name='Airlines'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM Aircraft) WHERE name='Aircraft'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM Flights) WHERE name='Flights'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM AspNetUsers) WHERE name='AspNetUsers'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM UserPreferences) WHERE name='UserPreferences'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM UserFlights) WHERE name='UserFlights'; \
UPDATE sqlite_sequence SET seq = (SELECT IFNULL(MAX(Id),0) FROM PersonalAccessTokens) WHERE name='PersonalAccessTokens'; \
DETACH DATABASE old; \
PRAGMA foreign_keys=ON;"
```

Notes:

- `INSERT OR IGNORE` keeps existing rows in root DB when primary keys collide.
- If both DBs changed the same row and you need field-level conflict resolution, use a custom merge script instead of `OR IGNORE`.

Notes:

- `FlightTracker.Web.csproj` already runs Sass/JS asset targets during `dotnet build`.
- `npm run watch` is intended for local development feedback loops.

## Roadmap Snapshot

(Full list: `doc/Plan.md`)

- Completed baseline
  - FlightStatus enum migration and service/DTO mapping foundation
  - Blazor Web foundation with Radzen and initial ApexCharts usage
  - Passport stats services and aggregation DTOs
  - Aviationstack airport live data integration
- Current focus
  - Provider hardening (config, retries/backoff, resilient fallbacks)
  - Expand Application tests (cancellation token and aggregation scenarios)
  - Preference-aware formatting across Passport and Flight details
  - API-first presentation flow (`FlightTracker.Api`) with versioned contracts and Blazor typed clients
- Next phase
  - OpenSky adapter implementation behind `IFlightDataProvider`
  - Flight track persistence and playback interpolation
  - CI quality gates (build/test/coverage enforcement)

## Contributing

1. Create an Issue via the structured form (choose correct type & fill acceptance criteria).
2. Create a feature branch (`feature/<short-name>`).
3. Follow architecture + style rules (`.github/copilot-instructions.md`).
4. Use PR template and satisfy all checklist items.
5. Update `doc/Plan.md` for new or completed tasks.

See `CONTRIBUTING.md` for full process and pitfalls.

## Repository Conventions

- Repository interfaces live in FlightTracker.Application and are implemented in FlightTracker.Infrastructure.
- Seed data is idempotent; do not embed production credentials or PII.
- Avoid premature generic repositories; prefer focused ones per aggregate root.
- Return contracts follow the policy documented in `doc/Architecture.md` (raw repository/client contracts, Result mapping in Application services).

## Planned Entities (Beyond MVP)

- Aircraft (registration, type, model)
- TrackPoint / Encoded track store
- User passport stats aggregate
- FlightSource (tracking data provenance)

## License

TBD (add a LICENSE file before public distribution).

## Disclaimer

Flight data usage may be subject to third-party API terms (OpenSky / FR24 / etc.). Ensure compliance before production deployment.

---
Happy building & clear skies ✈️
