# FlightTracker

A modern, multi-platform flight tracking application (web, desktop, mobile) built with .NET and Clean Architecture principles. Designed to evolve from a simple flight & airport data core into a richer experience (live map, passport/stats, playback, provider adapters like OpenSky and FR24).

## Status
Early foundation in place:
- Domain entities: Flight, Airport
- Application service interfaces (Flight & Airport)
- Infrastructure: EF Core DbContext (with Identity), repositories, seed data
- Development guidelines & contribution docs
- Task plan: see `doc/Plan.md`

> Next focus: move repository interfaces inward, add DTOs & services, introduce FlightStatus enum, OpenSky provider stub.

## Architecture (Clean Architecture Inspired)
Layering (inward dependencies only):
```
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

## Key Principles
- No EF/Core attributes in Domain.
- All async public methods end with `Async` and accept `CancellationToken` last.
- DTO boundary between Application -> Presentation (no leaking EF/Identity models).
- External providers (OpenSky, FR24, Aviationstack) hidden behind interfaces.
- Logging & persistence in Infrastructure only.

## Technology (Current / Planned)
| Concern | Tech |
|--------|------|
| Language | C# 12 / .NET 9 (preview packages currently) |
| Auth | ASP.NET Core Identity (int keys) |
| ORM | EF Core (preview) |
| UI (planned) | Blazor WebAssembly + Radzen + ApexCharts |
| Realtime (planned) | SignalR |
| Mapping (planned) | AutoMapper |
| External Flight Data (planned) | OpenSky first, later FR24 / Aviationstack |
| Caching (planned) | MemoryCache / Redis (later) |

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

## Getting Started (Current Backend Foundation)
Prerequisites: .NET 9 SDK (preview), (optional) SQLite tools.

Build solution:
```powershell
dotnet build
```

(When API project is added, typical run will be):
```powershell
# future
# dotnet run --project FlightTracker.Api
```

Add initial migration (after deciding on provider & stabilizing enum changes):
```powershell
# Example (future, once FlightStatus enum added and provider chosen):
# dotnet ef migrations add InitialCreate -p FlightTracker.Infrastructure -s FlightTracker.Api
```

## Roadmap Snapshot
(Full list: `doc/Plan.md`)
- Implement FlightStatus enum & refactor status field
- Application services + DTO mapping
- OpenSky adapter + provider abstraction
- API project (controllers, DI, Swagger, SignalR hub)
- Blazor Web front (Radzen + ApexCharts integration)
- Passport / Stats aggregation + chart output DTOs
- Flight track storage & playback interpolation

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
