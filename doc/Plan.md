# FlightTracker – Development Task Plan

Purpose: Track actionable work items only. Checked items = completed. Keep this lean; update as tasks move stages.

## Legend
- [x] Done
- [ ] Todo / Pending
- [ ] *Italic* = In progress
- (Deferred) suffix = Later phase

---
## 1. Core Architecture & Alignment
- [x] Establish solution + core projects (Domain, Application, Infrastructure)
- [x] Add Clean Architecture guidance (`.github/copilot-instructions.md`)
- [x] Repository interfaces live in `FlightTracker.Application` (per guidance)
- [x] Remove duplicate repository interfaces under `FlightTracker.Infrastructure/Repositories/Interfaces` (keep interfaces only in Application)
- [ ] Add project references for forthcoming Presentation projects (once created)
- [ ] Introduce consistent namespace + folder templates (scaffolding script / README section)

## 2. Domain Layer
- [x] Entities: Flight, Airport (POCO)
- [x] Introduce `FlightStatus` enum (replace string Status) — mapped via EF conversion in DbContext
- [ ] Add value object (e.g., AirportCode) to reduce string duplication (optional)
- [ ] Add domain exceptions (e.g., FlightNotFoundException)
- [ ] Introduce IClock abstraction reference boundary (interface only, implementation outward)
- [ ] Aircraft / TrackPoint / Passport stats aggregates (Deferred)

## 3. Application Layer
- [x] Service interfaces: IFlightService, IAirportService
- [ ] Implement FlightService (CRUD + upcoming flights query)
- [ ] Implement AirportService (CRUD + lookup by code)
- [ ] Create DTOs: FlightDto, FlightDetailDto, AirportDto, Create/Update variants
- [ ] Mapping profiles (AutoMapper) Domain <-> DTOs
- [ ] Add Result wrapper (e.g., Result<T>, PaginatedResult<T>)
- [ ] Add validation (FluentValidation or custom) for create/update DTOs
- [ ] Add cancellation token propagation tests
- [ ] Define external provider abstraction: IFlightDataProvider (live flight & track retrieval)
- [ ] Define IFlightStatsService + DTOs for passport/stats (TotalFlights, TotalHours, MonthlyCounts)
- [ ] Pagination & filtering strategy abstraction (FlightQueryOptions) (Deferred)

## 4. Infrastructure Layer
- [x] DbContext with Identity (ApplicationUser) + Flights/Airports
- [x] Repositories (FlightRepository, AirportRepository)
- [x] SeedData (airports + sample flights + users)
- [ ] Add initial EF Core migration (once provider finalized; current packages preview)
- [ ] Replace string Status with enum mapping (conversion)
- [ ] Implement repository interfaces in new folder structure after relocation decision
- [ ] Add EF configuration classes (separate from OnModelCreating) if model grows
- [ ] External API client scaffolds: OpenSkyClient (implements IFlightDataProvider)
- [ ] Add caching layer (MemoryCache) decorator for IFlightDataProvider
- [ ] Add background hosted service for periodic flight refresh (pull + persist)
- [ ] Introduce IClock implementation (UtcClock)
- [ ] Add configuration binding (appsettings) for external API keys / endpoints
- [ ] Logging strategy: structured logs for repository & provider operations
- [ ] Redis / distributed cache integration (Deferred)
- [ ] FR24 / Aviationstack provider adapters (Deferred)

## 5. Presentation – API (to be created)
- [ ] Create `FlightTracker.Api` project
- [ ] Configure DI (DbContext, Identity, Repositories, Services, Providers)
- [ ] Controllers: FlightsController, AirportsController, StatsController
- [ ] Model binding & validation responses (ProblemDetails)
- [ ] Global exception handling middleware
- [ ] Authentication & authorization (JWT or cookie) baseline
- [ ] Swagger / OpenAPI setup
- [ ] Versioning strategy (route or header) placeholder
- [ ] SignalR hub for live flight position updates
- [ ] Health checks endpoint

## 6. Presentation – Web (Blazor WASM)
- [x] Create `FlightTracker.Web` project (WASM)
- [x] Integrate Radzen components (only UI layer)
- [ ] Integrate ApexCharts for stats (passport charts)
- [x] Map component via JS interop (MapLibre or Leaflet) spike
- [ ] Flight list & selection panel
- [ ] Airport detail page
- [ ] Passport / Stats dashboard (charts + summary cards)
- [ ] State-based flight views (Pre-flight, At airport, Post-flight) UI workflow
- [ ] Auth UI (login/register) once API auth ready
- [ ] Light/Dark theme toggle + Material design tokens
- [ ] Offline caching (PWA capability) (Deferred)

## 7. Presentation – Desktop & Mobile (Future)
- [ ] Create MAUI Blazor Hybrid project (Deferred)
- [ ] Create MAUI Mobile project (shared Razor components) (Deferred)
- [ ] Platform-specific packaging / icons (Deferred)

## 8. External Data & Realtime
- [ ] Implement OpenSky adapter (basic: fetch flights in bounding box)
- [ ] Normalize OpenSky responses to internal DTOs
- [ ] Add rate limiting & backoff policy (Polly) for provider calls
- [ ] Implement SignalR broadcasting of updated flight positions
- [ ] Track ingestion pipeline (store TrackPoints or encoded polyline)
- [ ] Playback interpolation util (time-based position smoothing)
- [ ] Add FR24 provider abstraction implementation (after MVP) (Deferred)
- [ ] Historical playback (persisted tracks with timeframe query) (Deferred)

## 9. Passport / Stats Feature
- [x] Define stats aggregation queries (total hours, routes, aircraft types placeholder)
- [ ] Implement IFlightStatsService
- [ ] Add background job or on-demand recompute strategy
- [ ] Expose API endpoint /stats/user/{id}
- [ ] Provide chart-friendly DTOs (MonthlyFlightCounts etc.)
- [ ] Integrate charts in UI (ApexCharts) once Web project exists

## 10. Testing Strategy
- [ ] Add test projects: Domain.Tests, Application.Tests, Infrastructure.Tests
- [ ] Domain unit tests (Flight scheduling, status transitions once added)
- [ ] Application service tests (mock repositories/providers)
- [ ] Infrastructure integration tests (SQLite in-memory enforcing FKs)
- [ ] SeedData idempotency test
- [ ] Repository CRUD tests
- [ ] OpenSky provider adapter tests with canned JSON
- [ ] Stats aggregation tests (edge cases: zero flights, multiple months)
- [ ] Performance sanity test (batch fetch of flights)

## 11. Performance & Scaling
- [x] Add indexes (FlightNumber, Airport.Code) if not already in migrations
- [ ] Query pagination for large flight lists (>100 rows)
- [ ] Introduce caching decorator for read-heavy queries
- [ ] Evaluate memory footprint of live tracking (prune stale flights)
- [ ] Spatial indexing / PostGIS migration (if advanced geospatial needed) (Deferred)

## 12. Security & Auth
- [x] Identity schema migrations
- [ ] Password policy + account lockout config
- [ ] Role setup (User, Admin) & policy stubs
- [ ] Secure endpoints (JWT or cookie auth) + refresh strategy
- [ ] Input validation hardening (anti-overposting DTOs)
- [ ] Secrets management plan (user secrets / environment variables)
- [ ] 2FA / MFA support (Deferred)

## 13. Developer Experience
- [x] README draft (project purpose, quick start)
- [ ] Add solution-level build script (format, test, coverage)
- [ ] EditorConfig for consistent style
- [ ] GitHub Actions CI (build + test)
- [ ] Dependabot / NuGet package update workflow
- [ ] Issue templates / PR template
- [ ] Code coverage badge & threshold enforcement (Deferred)

## 14. Documentation & Diagrams
- [ ] Update architecture diagram (layers & dependencies)
- [ ] Document provider abstraction (OpenSky -> interface mapping)
- [ ] DTO mapping guide (Domain <-> DTO)
- [ ] Background jobs design note
- [ ] Flight state machine (Pre-flight → At airport → In-flight → Landed → Archived)
- [ ] Sequence diagram for live tracking broadcast (Deferred)

## 15. Future / Stretch Ideas
- [ ] Offline mode (local cache, sync) (Deferred)
- [ ] Notifications (pre-flight alerts, gate changes) via SignalR / push (Deferred)
- [ ] Badge / achievement system tied to stats (Deferred)
- [ ] Real aircraft image integration (external image API) (Deferred)
- [ ] Multi-tenant / organization support (Deferred)
- [ ] Export flights (CSV / ICS) (Deferred)
- [ ] User-configurable dashboards (Deferred)

---
## Immediate Next Suggested Focus
1. Remove duplicate repository interfaces from Infrastructure; codify Application as the single source for repository contracts.
2. Add test projects scaffold + initial Domain/Application tests (SeedData idempotency, MapFlightService basic projection).
3. Scaffold OpenSky provider abstraction + stub implementation (no outbound calls yet).
4. Optional: introduce mapping profiles (e.g., AutoMapper) for DTOs to reduce manual mapping noise.

Keep this file updated; prune completed groups to maintain clarity.
