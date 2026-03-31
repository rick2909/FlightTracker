# Repository and Result Policy

This file defines the return-contract boundary used in FlightTracker.

## Rule Summary

- Repositories and low-level external clients return raw values only.
- Application services map outcomes to Result or Result<T>.
- Presentation maps Result outcomes to HTTP or UI responses.

## Allowed Return Types by Layer

| Layer | Allowed contract |
| --- | --- |
| Repository / external client | T?, IReadOnlyList<T>, primitive values, or throws technical exceptions |
| Application service | Result or Result<T> |
| Presentation | ActionResult / HTTP status codes / view model state |

## Mapping Guidance

- Missing optional record: return Result.Success(null) when null is a valid domain outcome.
- Missing required record: return Result.Failure with deterministic not-found error code.
- Infrastructure exception: catch in Application and translate to Result.Failure.
- Validation or business rule violation: return Result.Failure with deterministic validation/business error code.

## Separation of Concerns

- Domain and Application do not depend on EF or transport concerns.
- Presentation does not call repositories directly.
- Error-code vocabulary is owned by the Application layer.

## Why

This keeps persistence concerns simple, centralizes business error semantics in one layer, and prevents Result-wrapper leakage into data access code.
