# FlightTracker API Versioning And Deprecation Policy

## Scope

This policy applies to all HTTP endpoints served by FlightTracker.Api.
Current public API base path is /api/v1.

## Stateless And Client-Agnostic Contract Rules

- API endpoints must remain stateless between requests.
- Authentication context is carried only by bearer tokens.
- No cookie-session coupling is required for API calls.
- Contracts in FlightTracker.Api/Contracts/V1 must not include Web-only view model assumptions.
- DTO evolution must preserve cross-client compatibility for Web, MAUI, and other first-party clients.

## Versioning Strategy

- Route-based versioning is used.
- Major version is encoded in the URL prefix, for example: /api/v1.
- Minor, additive changes (new optional fields, new endpoints) may ship in the same major version when non-breaking.
- Breaking changes require a new major route prefix (for example /api/v2).

## Deprecation Policy

- Deprecated major versions remain available for at least one release cycle after successor GA.
- During deprecation, API responses include:
  - api-supported-versions
  - api-deprecation-notes
- For each deprecation event, release notes must include:
  - impacted routes
  - migration guidance
  - retirement target date

## Non-Breaking Evolution Rules

Allowed in a live major version:

- Add new endpoints.
- Add new optional request fields.
- Add new optional response fields.

Not allowed in a live major version:

- Remove existing fields.
- Change field types.
- Change endpoint semantics or status code behavior in a breaking way.

## First-Party Authentication Baseline

- First-party API clients use Authorization: Bearer {token}.
- Token validation configuration lives under Authentication:Bearer in API appsettings.
- Future authorization scopes and policies can be added without changing this baseline.
