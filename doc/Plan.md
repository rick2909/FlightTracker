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
- [x] Add project references for Presentation project `FlightTracker.Web` (Blazor Server)
- [ ] Introduce consistent namespace + folder templates (scaffolding script / README section)

## 2. Domain Layer

- [x] Entities: Flight, Airport (POCO)
- [x] Introduce `FlightStatus` enum (replace string Status) — mapped via EF conversion in DbContext
- [x] Add UserPreferences entity with display & unit enums (DistanceUnit, TemperatureUnit, TimeFormat, DateFormat)
- [x] Add ProfileVisibilityLevel enum for privacy settings
- [ ] Add value object (e.g., AirportCode) to reduce string duplication (optional)
- [ ] Add domain exceptions (e.g., FlightNotFoundException)
- [ ] Introduce IClock abstraction reference boundary (interface only, implementation outward)
- [ ] Aircraft / TrackPoint / Passport stats aggregates (Deferred)

## 3. Application Layer

- [x] Service interfaces: IFlightService, IAirportService
- [x] Implement FlightService (CRUD + upcoming flights query)
- [x] Implement AirportService (CRUD + lookup by code)
- [x] Create DTOs: FlightDto, FlightDetailDto, AirportDto
- [x] Create/Update DTO variants (UserFlight Update, Flight schedule update)
- [x] Mapping profiles (AutoMapper) Domain <-> DTOs
- [ ] Add Result wrapper (e.g., Result&lt;T&gt;, PaginatedResult&lt;T&gt;)
- [x] Add validation (FluentValidation) for create/update DTOs (CreateUserFlightDto, UpdateUserFlightDto, FlightScheduleUpdateDto)
- [ ] Add cancellation token propagation tests
- [x] Define external provider abstraction: IFlightDataProvider (live flight & track retrieval)
- [x] Define airport live data abstraction: IAirportLiveService (departures/arrivals)
- [x] Add AirportOverviewService (merges DB + live provider results with de-duplication)
- [x] Add IUserPreferencesService + UserPreferencesService for user settings management
- [x] Add UserPreferencesDto with display, unit, and privacy fields
- [ ] Define IFlightStatsService + DTOs for passport/stats (TotalFlights, TotalHours, MonthlyCounts)
- [ ] Add aggregation DTOs for Passport details: AirlineStatsDto { AirlineName, AirlineIata, Count }, AircraftTypeStatsDto { TypeCode, Count }
- [ ] Implement IFlightStatsService (group-by airline and aircraft type for a user)
- [ ] Extend PassportDataDto or add PassportDetailsDto to include AirlineStats and AircraftTypeStats (used by Passport details view)
- [ ] Pagination & filtering strategy abstraction (FlightQueryOptions) (Deferred)
 - [x] Add IPassportService + PassportService (aggregate stats + routes)
 - [x] Add PassportDataDto (single payload for Passport page)

## 4. Infrastructure Layer

- [x] DbContext with Identity (ApplicationUser) + Flights/Airports
- [x] Repositories (FlightRepository, AirportRepository, UserPreferencesRepository)
- [x] SeedData (airports + sample flights + users)
- [x] Add initial EF Core migration
- [x] Replace string Status with enum mapping (conversion)
- [x] Implement repository interfaces in new folder structure after relocation decision
- [x] Add UserPreferences table with migrations (AddUserPreferences, AddPrivacySharingSettings)
- [ ] Add EF configuration classes (separate from OnModelCreating) if model grows
- [ ] External API client scaffolds: OpenSkyClient (implements IFlightDataProvider) (Deferred)
- [ ] Add caching layer (MemoryCache) decorator for IFlightDataProvider
- [ ] Add background hosted service for periodic flight refresh (pull + persist)
- [ ] Introduce IClock implementation (UtcClock)
- [x] Add configuration binding (appsettings) for external API keys / endpoints (Aviationstack:ApiKey)
- [ ] Logging strategy: structured logs for repository & provider operations
- [ ] Redis / distributed cache integration (Deferred)
- [x] Aviationstack provider adapter (IAirportLiveService implementation) for airport departures/arrivals
- [ ] FR24 provider adapter (Deferred)

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

## 6. Presentation – Web (Blazor Server)

- [x] Create `FlightTracker.Web` project (WASM)
- [x] Integrate Radzen components (only UI layer)
- [x] Choose charting library: ApexCharts
- [ ] *Integrate ApexCharts for stats (passport charts)*
- [x] Map component via JS interop (MapLibre or Leaflet) spike
- [x] Flight list & selection panel (Airports page: departing/arriving)
- [x] Airport overview/detail page (map + lists; initial version with live toggle)
- [x] Settings page (profile, password, display & units preferences, privacy & sharing, CSV/JSON export)
- [x] Wire Airports UI to AirportOverviewService with optional Aviationstack live data
- [ ] NEW: Passport details view (more in-depth stats inside Passport)
	- [x] Route: `/Passport/{id?}` gains a "Details" tab/section (or `/Passport/{id?}/Details` action)
	- [x] Two ApexCharts pie charts
		- [x] Airlines flown (click to filter list)
		- [x] Aircraft types flown (click to filter list)
	- [x] Filtered lists beneath charts (respect selected airline/type)
	- [x] Data source: IFlightStatsService with AirlineStatsDto & AircraftTypeStatsDto via PassportDetailsDto/PassportDataDto
	- [ ] Accessibility: chart labels, table summaries, keyboard navigation
	- [ ] Performance: AsNoTracking queries; scoped to current user; defer pagination initially
- [x] Passport / Stats dashboard (charts + summary cards)
		- [x] Summary cards bound to DB data via IPassportService
		- [x] Flights Per Year chart (ApexCharts)
		- [x] Pie chart: Airlines flown (ApexCharts)
		- [x] Pie chart: Aircraft types flown (ApexCharts)
	- [x] *Charts wiring (ApexCharts) and interactions*  
			Note: Scripts included; finalize dataset + rendering hooks
- [x] State-based flight views (Pre-flight, At airport, Post-flight) UI workflow
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
- [x] Aviationstack departures/arrivals wired to Airports page via IAirportLiveService

## 9. Passport / Stats Feature

- [x] Define stats aggregation queries (total hours, routes, aircraft types placeholder)
- [ ] Implement IFlightStatsService
- [ ] Add background job or on-demand recompute strategy
- [ ] Expose API endpoint /stats/user/{id}
- [ ] Provide chart-friendly DTOs (MonthlyFlightCounts etc.)
- [ ] Integrate charts in UI (ApexCharts) once Web project exists
 - [x] Implement IPassportService returning PassportDataDto (aggregates + routes)
 - [x] Wire Web to DB (replace mock): `PassportController` maps PassportDataDto => PassportViewModel
 - [x] Routing: support `/passport/{id?}` with fallback to current user or redirect when unauthenticated
 - [ ] Add privacy/sharing option for viewing someone else’s passport (Deferred)
 - [ ] Normalize country codes to ISO-3166-1 alpha-2 for flag rendering

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
- [x] Username validation service with business rules
- [ ] Password policy + account lockout config
- [ ] Role setup (User, Admin) & policy stubs
- [ ] Registration flow: verify email before collecting username/password
- [ ] Secure endpoints (JWT or cookie auth) + refresh strategy
- [ ] Input validation hardening (anti-overposting DTOs)
- [ ] Secrets management plan (user secrets / environment variables)
- [ ] 2FA / MFA support (Deferred)

## 13. User Settings & Preferences

- [x] Settings page baseline (profile, password, theme, visibility)
- [x] **Display & Units Preferences**
	- [x] Add DistanceUnit, TemperatureUnit, TimeFormat, DateFormat enums
	- [x] Create UserPreferences entity with database storage
	- [x] Implement repository and service layers
	- [x] Add UI controls in Settings → Preferences tab
	- [x] Migrate from cookie-based to database storage
	- [ ] Create helper services/formatters to use preferences throughout app
	- [ ] Update Passport page to display distances in user's preferred unit
	- [ ] Apply date/time formatting preferences in flight details
- [ ] **Privacy & Sharing Settings**
	- [x] Enhance ProfileVisibility with granular levels (Public/Private)
	- [x] Add StatsVisibility flags (show/hide total miles, airlines, countries)
	- [x] Add MapVisibility toggle (show/hide routes on public profile)
	- [x] Add ActivityFeed preference (enable/disable flight sharing)
	- [x] Create ProfileVisibilityLevel enum
	- [x] Add privacy fields to UserPreferences entity and database
	- [x] Update UI with toggle switches in Settings → Preferences tab
	- [ ] Implement access control logic for public profile views
	- [ ] Apply privacy settings when rendering Passport page
- [ ] **Notification Preferences** (Deferred)
	- [ ] Email notifications toggle
	- [ ] Flight update alerts preference
	- [ ] Weekly digest opt-in
	- [ ] Browser push notification settings
- [ ] **Data & API Integration Settings** (Deferred)
	- [ ] User-provided API keys (OpenSky, FR24, Aviationstack)
	- [ ] Auto-sync toggle for external enrichment
	- [ ] Data refresh frequency selector
	- [ ] API usage/quota display
- [ ] **Passport Customization** (Deferred)
	- [ ] Avatar/profile photo upload
	- [ ] Home airport preference
	- [ ] Favorite airline pin
	- [ ] Travel goals (yearly flight/distance targets)
	- [ ] Badge visibility toggles
- [ ] **Map Preferences** (Deferred)
	- [ ] Map style selector (Light/Dark/Satellite/Terrain)
	- [ ] Default zoom level
	- [ ] Route display options (all/recent X flights)
	- [ ] Clustering toggle
- [ ] **Import/Export Enhancements** (Deferred)
	- [ ] Auto-backup frequency (Daily/Weekly/Monthly)
	- [ ] PDF export with charts
	- [ ] Import from other flight trackers (standardized format)

## 14. Developer Experience

- [x] README draft (project purpose, quick start)
- [ ] Add solution-level build script (format, test, coverage)
- [ ] EditorConfig for consistent style
- [ ] GitHub Actions CI (build + test)
- [ ] Dependabot / NuGet package update workflow
- [ ] Issue templates / PR template
- [ ] Code coverage badge & threshold enforcement (Deferred)

## 15. Documentation & Diagrams

- [ ] Update architecture diagram (layers & dependencies)
- [ ] Document provider abstraction (OpenSky -> interface mapping)
- [ ] DTO mapping guide (Domain <-> DTO)
- [ ] Background jobs design note
- [ ] Flight state machine (Pre-flight → At airport → In-flight → Landed → Archived)
- [ ] Sequence diagram for live tracking broadcast (Deferred)

## 16. Future / Stretch Ideas

- [ ] Offline mode (local cache, sync) (Deferred)
- [ ] Notifications (pre-flight alerts, gate changes) via SignalR / push (Deferred)
- [ ] Badge / achievement system tied to stats (Deferred)
- [ ] Real aircraft image integration (external image API) (Deferred)
- [ ] Multi-tenant / organization support (Deferred)
- [ ] Export flights (CSV / ICS) (Deferred)
- [ ] User-configurable dashboards (Deferred)

---

## Immediate Next Suggested Focus

1. Wrap up Flight details/edit (done): unified update use case, validators, Web wired.
2. Optional: Add Result wrapper for APIs; improve error surface in Presentation.
3. Prepare Passport details spec (not implementation): define IFlightStatsService contract + AirlineStatsDto/AircraftTypeStatsDto and outline Passport UI (Details tab + charts and filtered lists).
4. Provider hardening: ensure config key set via appsettings; keep Polly retry/backoff and consider a simple enable/disable flag for live providers.
5. Add unit tests plan for forthcoming IFlightStatsService (grouping correctness, empty dataset, large dataset) and for AirportOverviewService merge logic.

Keep this file updated; prune completed groups to maintain clarity.
