# FlightTracker

[![Build](https://img.shields.io/github/actions/workflow/status/rick2909/FlightTracker/ci.yml?branch=main&style=for-the-badge&label=build)](https://github.com/rick2909/FlightTracker/actions)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Sass](https://img.shields.io/badge/Sass-CC6699?style=for-the-badge&logo=sass&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-5A2B81?style=for-the-badge&logo=xunit&logoColor=white)
[![Coverage](https://img.shields.io/codecov/c/github/rick2909/FlightTracker?branch=main&style=for-the-badge&logo=codecov)](https://app.codecov.io/gh/rick2909/FlightTracker)

A modern, multi-platform flight tracking application (web, desktop, mobile) built with .NET and Clean Architecture principles. Designed to evolve from a simple flight & airport data core into a richer experience (live map, passport/stats, playback, provider adapters like OpenSky and FR24).

## Status

Early foundation in place:

- Domain entities: Flight, Airport
- Application service interfaces (Flight & Airport)
- Infrastructure: EF Core DbContext (with Identity), repositories, seed data
- Development guidelines & contribution docs
- Task plan: see `doc/Plan.md`

> Next focus: Provider hardening (Aviationstack key via user-secrets, retries), tests for merge logic, OpenSky adapter stub.

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
├─ FlightTracker.Web/
│  ├─ Components/
│  ├─ Controllers/
│  ├─ Models/
│  ├─ Styling/
│  ├─ Views/
│  └─ wwwroot/
├─ Tests/
│  ├─ FlightTracker.Application.Tests/
│  ├─ FlightTracker.Domain.Tests/
│  ├─ FlightTracker.Infrastructure.Tests/
│  └─ FlightTracker.Web.Tests/
└─ doc/
```

## CI and Quality Notes

- Build badge targets GitHub Actions workflow file `ci.yml` on `main`.
- Coverage badge targets Codecov for `rick2909/FlightTracker` on `main`.
- If your workflow file uses a different name, update the badge URL segment `ci.yml`.

## Key Principles

- No EF/Core attributes in Domain.
- All async public methods end with `Async` and accept `CancellationToken` last.
- DTO boundary between Application -> Presentation (no leaking EF/Identity models).
- External providers (OpenSky, FR24, Aviationstack) hidden behind interfaces.
- Logging & persistence in Infrastructure only.
- Repositories and low-level external clients return raw data/null/collections; Application services map outcomes to `Result`/`Result<T>`.

See `doc/Repository-Result-Policy.md` for the authoritative return-contract policy.

## Technology (Current / Planned)

| Concern | Tech |
| ------- | ---- |
| Language | C# 13 / .NET 10 |
| Auth | ASP.NET Core Identity (int keys) |
| ORM | EF Core 10 |
| UI (current) | ASP.NET Core MVC + Blazor Server, Radzen, ApexCharts |
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
  - API project scaffolding (`FlightTracker.Api`) with DI and controllers
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

- Repositories currently located in Infrastructure (Interfaces + Implementation) — scheduled to move interfaces inward per plan.
- Seed data is idempotent; do not embed production credentials or PII.
- Avoid premature generic repositories; prefer focused ones per aggregate root.
- Return contracts follow `doc/Repository-Result-Policy.md` (raw repository/client contracts, Result mapping in Application services).

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
