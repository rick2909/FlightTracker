# Repository & Result Policy

## Purpose
Define a consistent error/return strategy that preserves Clean Architecture boundaries and keeps contracts predictable.

## Core Decision
- Repositories and low-level external clients **should return data models (or null/empty)**, not application `Result` wrappers.
- Application services (use-case layer) **own Result mapping** and return `Result` / `Result<T>` to Presentation.

This keeps persistence/integration contracts simple and places user-facing outcome semantics in the Application layer.

## Layer Rules

### Domain
- No `Result` dependency.
- Domain logic may throw domain-specific exceptions.

### Application
- Services/orchestrators return `Result` / `Result<T>`.
- Catch and map infrastructure/external failures to stable error codes/messages.
- Convert null/not-found to either `Result.Success(null)` or a failure code depending on use-case semantics.

### Infrastructure
- Repository implementations return entities/collections/null and may throw technical exceptions.
- External client implementations return DTOs/collections/null and may throw technical exceptions.
- No presentation-specific status behavior.

### Presentation (Web/API)
- Never call repositories directly.
- Consume Application service `Result` values and map to HTTP/UI behavior.

## Contract Guidance

### Repository Interfaces
- Read-by-id/code: return nullable entity (`Task<Entity?>`).
- List/search: return collections (`Task<IReadOnlyList<Entity>>`), empty list if no data.
- Commands (add/update/delete): return entity or completion, based on need.
- Do not leak framework-specific transport semantics.

### Application Service Interfaces
- Use `Result<T>` for data-returning operations.
- Use `Result` for command operations.
- Include stable `ErrorCode` values for failure paths.

## Error Mapping Strategy
- Infrastructure/client exception -> caught in Application service -> `Result.Failure(errorMessage, errorCode)`.
- Not found:
  - Query-style use-cases: usually `Result.Success(null)`.
  - Command/business rule failures: `Result.Failure(..., "*.not_found")` when required by UX/API contract.
- Validation/business rule violations: return failure with deterministic error code.

## Keep It Clean Checklist
- Is this interface in Repository/External Client scope?
  - Return raw data/null/collections.
- Is this interface in Application service/use-case scope?
  - Return `Result` / `Result<T>`.
- Are error codes created only in Application?
  - Yes.
- Are controllers/components unwrapping only service Results?
  - Yes.

## FlightTracker Convention (effective now)
- Existing Application services may continue returning `Result` / `Result<T>`.
- New repositories should remain raw-return contracts.
- When refactoring, prefer moving Result mapping upward (toward Application), not downward.
