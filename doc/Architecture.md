# FlightTracker — Architecture & Policy Reference

## Clean Architecture Layers

| Layer | Responsibility | Dependencies |
|---|---|---|
| **Domain** | Entities, enums, value objects | None |
| **Application** | Use cases, services, DTOs | Domain only |
| **Infrastructure** | EF Core, repositories, external clients | Application, Domain |
| **Presentation** | API, Blazor Web, MAUI | Application |

- No layer depends outward. Domain is fully independent.
- Outer layers implement interfaces defined inward.
- UI/framework specifics (Radzen, ApexCharts, Identity) never enter Domain or Application.
- Application layer returns DTOs only across boundaries — never EF entities or Identity models.

Default presentation architecture: **API + Blazor Server**.
Legacy MVC compatibility controllers are deprecated and retained only during the API migration window.

---

## Repository & Result Policy

- **Repositories** return raw data only: `Entity?`, `IReadOnlyList<Entity>`, or throw technical exceptions. Never wrap returns in `Result`.
- **Application services** own `Result` / `Result<T>` mapping and return it to Presentation.
- **Presentation** consumes `Result` and maps to HTTP responses or UI behavior.
- Controllers must never call repositories directly.

### Return contracts by layer

| Layer | Contract |
|---|---|
| Repository / External client | `T?` / `IReadOnlyList<T>` / throws |
| Application service | `Result` / `Result<T>` |
| Presentation | HTTP 200 / 400 / 404 / 500 |

### Error mapping

- **Null from repo** → `Result.Success(null)` for optional queries, `Result.Failure("*.not_found")` for required resources.
- **Infrastructure exception** → caught in Application → `Result.Failure(message, errorCode)`.
- **Validation / business rule violation** → `Result.Failure(message, deterministicErrorCode)`.
- Error codes are created only in the Application layer.

---

## Authentication

- **Web frontend** — cookie-based authentication via ASP.NET Core Identity.
- **Web → API typed clients** — short-lived first-party JWTs issued by the Web host for authenticated users; attached as `Authorization: Bearer {token}`.
- **External / third-party clients** — Personal Access Tokens with prefix `ft_pat_*`.
- **API** validates bearer tokens and enforces user-route ownership (`userId` in route must match the authenticated token subject).

---

## Personal Access Token (PAT) Policy

### Security baseline

- Tokens are shown **once** at creation time; only their hash is stored in the database.
- Raw token values are never persisted.
- Revoked and expired tokens are rejected immediately.

### Supported scopes

| Scope | Access |
|---|---|
| `read:flights` | Read user flight history |
| `write:flights` | Create and modify flight records |
| `read:stats` | Read flight statistics and passport data |

### Operational controls

- PAT-authenticated calls are rate-limited to **120 requests / minute** per token.
- IP-based fallback rate limit: **600 requests / minute**.
- Token usage is recorded via audit log events; last-used timestamp updated on each successful call.

### Future

OAuth2/OIDC app registration with delegated flows is reserved as the next stronger model.

---

## API Versioning & Deprecation

- Route-based versioning: `/api/v1`, `/api/v2`, etc.
- **Non-breaking changes** (new optional fields, new endpoints) ship within the same major version.
- **Breaking changes** (removed fields, type changes, semantic changes) require a new major prefix.
- Deprecated versions remain available for at least one release cycle after the successor reaches GA.
- Deprecated responses include `api-supported-versions` and `api-deprecation-notes` headers.

---

## API Client Surface (v1)

Base URL: `https://{host}/api/v1`
Auth header: `Authorization: Bearer {token}`

| Group | Endpoints |
|---|---|
| Airports | `GET /airports`, `GET /airports/{code}`, `GET /airports/{code}/flights` |
| Flights | `GET /flights/{id}`, `GET /flights/upcoming?fromUtc=&windowHours=` |
| Passport | `GET /passport/users/{userId}`, `GET /passport/users/{userId}/details` |
| Stats | `GET /stats/users/{userId}/passport-details` |
| Preferences | `GET /preferences/users/{userId}`, `PUT /preferences/users/{userId}` |
| User Flights | `GET /users/{userId}/flights`, `POST /users/{userId}/flights`, `GET /users/{userId}/flights/stats`, `GET /user-flights/{id}`, `PUT /user-flights/{id}`, `DELETE /user-flights/{id}` |
| Access Tokens | `GET /users/{userId}/access-tokens`, `POST /users/{userId}/access-tokens`, `POST /users/{userId}/access-tokens/revoke` |

Contracts: `FlightTracker.Api/Contracts/V1`
Application DTOs: `FlightTracker.Application/Dtos`

---

## Folder Conventions

```
Domain/           Entities/, Enums/
Application/      Dtos/, Services/Interfaces/, Services/Implementation/
Infrastructure/   Data/ (DbContext, Migrations, SeedData), Repositories/, External/, Time/
Presentation/     Api/, Web/, Desktop/, Mobile/
Tests/            *.Domain.Tests/, *.Application.Tests/, *.Infrastructure.Tests/, *.Web.Tests/
```

---

## SQLite Location and Migration Commands

Canonical development database file:

- Solution root: `./flighttracker.dev.db`

Runtime behavior:

- API runs with working directory `FlightTracker.Api`
- API connection string therefore uses `../flighttracker.dev.db`

Apply migrations to the root DB:

```powershell
dotnet ef database update \
	--project .\FlightTracker.Infrastructure\FlightTracker.Infrastructure.csproj \
	--startup-project .\FlightTracker.Api\FlightTracker.Api.csproj \
	--context FlightTrackerDbContext \
	--connection "Data Source=$((Resolve-Path .\flighttracker.dev.db).Path)"
```

Create a migration in Infrastructure:

```powershell
dotnet ef migrations add <MigrationName> \
	--project .\FlightTracker.Infrastructure\FlightTracker.Infrastructure.csproj \
	--startup-project .\FlightTracker.Api\FlightTracker.Api.csproj \
	--context FlightTrackerDbContext \
	--output-dir Data\Migrations
```
