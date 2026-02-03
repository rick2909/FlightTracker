# FlightTracker – Copilot & Agent Instructions

## Architecture Overview
Adopt Clean Architecture with inward dependencies only:

1. Domain
	- Pure business entities & value objects (NO EF attributes).
	- Core domain logic only.
2. Application
	- Use cases (services), orchestration, validation.
	- DTOs for input/output (do not expose EF entities outward).
	- Depends ONLY on Domain.
3. Infrastructure
	- EF Core DbContext, configurations, repositories, external API clients.
	- Implements interfaces from Domain/Application.
4. Presentation
	- API (ASP.NET Core Web API), Web (Blazor WASM + Radzen + ApexCharts), Desktop/Mobile (MAUI Blazor Hybrid / MAUI).

Dependency Rule:
- No layer depends outward. Domain is independent.
- Outer layers implement interfaces defined inward.
- No UI/framework specifics inside Domain or Application.

## Development Steps (Roadmap)
1. Define Domain entities, enums, repository interfaces.
2. Implement Application use cases & DTOs.
3. Add Infrastructure persistence (EF Core) + repository implementations.
4. Integrate external data sources (OpenSky, FR24) via abstractions.
5. Expose endpoints in API project.
6. Build UI pages/components (Web) with Radzen & ApexCharts.
7. Add state management (e.g., upcoming flights, airport cache).
8. Implement passport/stats feature & charts.
9. Unit test Domain & Application; integration test Infrastructure.

## C# Code Style
- Microsoft conventions; 4 spaces indentation.
- Allman braces.
- Line length guideline: ~65 chars for docs (soft limit).
- Break long expressions before binary operators.
- Naming:
  - Interfaces: IName
  - Attributes: SomethingAttribute
  - Enums: singular (non-flags), plural (flags)
  - Private fields: _camelCase
  - Static fields: s_PascalCase
  - Classes/Methods/Constants: PascalCase
  - Locals/params: camelCase
- No double underscores. Avoid unnecessary abbreviations.
- Prefer descriptive names over terse ones.
- Single-letter vars only for tiny loop scopes.
- Minimize nested conditionals; prefer guard clauses and small helper methods to keep code flat and readable.

## SCSS Guidelines
- **Location**: All SCSS source files in `FlightTracker.Web/Styling/scss`
- **Build**: Compile via `npm run build:css` (Sass → CSS, no source maps)
- **Watch mode**: Use `npm run watch:css` during development
- **Output**: Compiled CSS goes to `FlightTracker.Web/wwwroot/css/`
- **NO direct CSS files** – all styling must be written in SCSS
- BEM naming: .block, .block__element, .block--modifier
- Max nesting depth: 3
- Centralize variables in _variables.scss (colors, spacing, fonts)
- Use mixins for repeated patterns (e.g., media queries, flex center)
- Avoid !important (last resort)
- Property order: positioning → box model → typography → visual → misc
- Use rem units (except borders)
- Keep component SCSS co-located with component

## JavaScript / TypeScript
- **Location**: JS files only in `FlightTracker.Web/Styling/JS`
- **Build**: Compile via `npm run build:js`
- **Watch mode**: Use `npm run watch:js` during development
- **Output**: Built JS goes to `FlightTracker.Web/wwwroot/js/`
- Prefer TypeScript. ES modules only.
- Naming: camelCase (vars/functions), PascalCase (classes), UPPER_CASE (consts)
- Small, single-responsibility functions.
- Prefer const; avoid var.
- Use template literals over concatenation.
- Always handle potential null/undefined.
- JSDoc for public functions.
- Minimize direct DOM manipulation; prefer Blazor interop for UI.
- Enforce via ESLint + TS plugin.

## Build & Development Commands
- `npm run build:css` – Compile SCSS to CSS (manual; use during development)
- `npm run watch:css` – Watch SCSS for changes (development only)
- `npm run build:js` – Copy/build JavaScript files (manual; use during development)
- `npm run watch:js` – Watch JavaScript for changes (development only)
- `npm run build` – Build both CSS and JS manually
- `npm run watch` – Watch both CSS and JS for changes

**Important**: The FlightTracker.Web.csproj includes MSBuild targets that automatically compile SCSS and copy JS during `dotnet build`. These npm commands are for local development with watch mode. Do NOT duplicate builds—use `dotnet build` for CI/CD or full builds, and npm watch commands for local file watching.

## Copilot & Agent Rules
- Respect architecture boundaries; business logic stays out of API/UI.
- DO NOT add EF Core attributes to Domain entities.
- Repository interfaces live in Domain (pure) or Application (choose ONE and stay consistent). CURRENT: Domain keeps entities, Application owns service interfaces; repository interfaces SHOULD reside in Application to prevent domain pollution with persistence concerns.
- All external dependencies accessed via interfaces (defined inward).
- Application layer returns DTOs (never EF entities or Identity models) when crossing to Presentation.
- UI-specific libraries (Radzen, ApexCharts) only in Presentation.
- Place new files in the correct layer/folder by responsibility.
- Before generating code, validate which layer it belongs to.
- Seed & test data: use Infrastructure SeedData or in-memory providers.
- Identity types (ApplicationUser, roles) remain in Infrastructure only; never leak to Domain/Application outputs.
- Migrations run from Infrastructure project only.
- All async public methods end with 'Async' and accept CancellationToken last with default = default.
- Propagate CancellationToken through layers; do not ignore.
- Do not introduce a generic repository unless explicitly requested; prefer focused repositories per aggregate.
- Prefer enums (e.g., FlightStatus) over magic strings; temporary strings must be centralized for easy refactor.
- Introduce time-dependent logic via an IClock abstraction (later) rather than DateTime.UtcNow directly in Domain/Application.
- Error handling: Domain may throw domain-specific exceptions; Application maps to Result/DTO; Presentation translates to HTTP responses.
- Logging only in Infrastructure & Presentation; Domain/Application stay free of logging frameworks.
- Avoid circular dependencies; Infrastructure must NOT be referenced by Domain or Application.

### Readability & Control Flow
- Keep methods small and focused.
- Prefer early returns over deep nesting.
- Extract validation and mapping into local functions or private helpers when it simplifies the primary flow.

### Folder Conventions (Illustrative)
Domain/
	Entities/
	Enums/
Application/
	Dtos/
	Services/Interfaces/
	Services/Implementation/ (application logic only)
Infrastructure/
	Data/ (DbContext, Migrations, SeedData)
	Repositories/Interfaces/
	Repositories/Implementation/
Presentation/
	Api/
	Web/
	Desktop/
	Mobile/

### DTO Guidance
- Never expose internal IDs you don't want public; map as needed.
- Flatten navigation properties; avoid passing full object graphs.
- Use records or immutable classes for DTOs where feasible.

### Seeding & Test Data
- SeedData.EnsureSeededAsync for dev/local only.
- Integration tests: call SeedData.SeedAsync against isolated database (SQLite in-memory recommended for FK enforcement).

### Performance & Querying
- Use AsNoTracking for read-only queries.
- Add indexes in OnModelCreating for frequent lookups (e.g., FlightNumber, Airport.Code).
- Paginate lists > 100 rows; do not return large unbounded lists.

### Security
- Do not store PII or secrets in Domain models unencrypted.
- Use Identity for auth; authorization policies defined in Presentation layer.

### PR / Commit Guidelines
- One feature or fix per commit/PR.
- Include or update tests for each new use case.
- Keep diffs minimal; avoid unrelated formatting churn.

### Code Generation Checklist (Copilot)
1. Determine correct layer.
2. Confirm naming & folder path.
3. Add interface before implementation (inward defines contract).
4. Add tests (Domain/Application) if behavior added.
5. Ensure async + CancellationToken compliance.
6. Return DTOs across boundaries.
7. Update SeedData only if expanding stable sample set.
8. Run build + tests before finalizing.

## Testing Guidance
- Domain: pure unit tests (no infrastructure).
- Application: service/use case tests with mocked repositories.
- Infrastructure: integration tests (e.g., SQLite in-memory) for EF & external clients.
- Avoid brittle time-based tests; use deterministic base times.
- Inject clock abstraction when time-sensitive logic emerges.

## Extension / Future Work
- Add caching layer (e.g., MemoryCache) via interface abstraction.
- Introduce FlightStatus enum & migration.
- Implement background refresh of flight positions.
- Add role-based authorization policies.
- Introduce IClock & caching abstractions.
- Add background job scheduling (e.g., refresh flight positions) via hosted service.
- Implement pagination & filtering strategies for large flight sets.

---
When generating or modifying code, adhere strictly to the above. If unsure about placement, prefer asking or default to the most inward valid layer.

When performing a code review, focus on readability and avoid nested ternary operators.

