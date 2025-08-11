# Contributing Guide – FlightTracker

This document explains how to create Issues and Pull Requests using the provided templates, and the workflow expectations aligned with the Clean Architecture rules defined in `.github/copilot-instructions.md`.

---
## Table of Contents
1. Workflow Overview
2. Branching Convention
3. Issue Form Usage
4. Pull Request Template Usage
5. Definition of Done
6. Labels & Status Suggestions
7. Architectural Boundaries Recap
8. Common Pitfalls to Avoid

---
## 1. Workflow Overview
1. Create an Issue using the Issue Form (Task / Feature / Bug / Refactor / Documentation).
2. Add clear Acceptance Criteria before starting implementation.
3. Create a feature branch from `master` (or `main` once renamed).
4. Implement changes with small, focused commits.
5. Open a Pull Request referencing the Issue (`Closes #<issue-number>`).
6. Ensure checklist in PR is satisfied (tests, DTO rules, architecture boundaries).
7. Merge only after review + green build (future CI).

---
## 2. Branching Convention
Proposed (lightweight):
- `main` (or `master` currently): stable, releasable.
- `feature/<short-kebab-summary>` for new functionality.
- `fix/<short-kebab-summary>` for bug fixes.
- `chore/` or `docs/` prefixes for non-functional changes.

Examples:
- `feature/flight-status-enum`
- `feature/open-sky-adapter`
- `fix/airport-null-code-bug`

Avoid long branch names; prefer clarity + brevity.

---
## 3. Issue Form Usage
The Issue Form (`.github/ISSUE_TEMPLATE/issue_form.yml`) drives consistency.

Field Guidance:
- **Issue Type**: Pick ONE (Bug / Feature / Task / Refactor / Documentation).
- **Description**: Problem statement or feature intent. Avoid solution bias for Features.
- **Affected Layer(s)**: Mark only layers touched by the change. If unsure, pick the most inward legitimate layer.
- **Acceptance Criteria**: Bullet list; each item must be objectively verifiable.
  - Example: `- Given an existing flight, when I request GET /api/flights/{id}, then I receive a 200 with FlightDetailDto.`
- **Reproduction Steps** (Bugs only): Exact sequence; include sample inputs/data if applicable.
- **Technical Notes**: Optional design, risks, data shape decisions.
- **Clean Architecture Compliance**: Check only if true; do NOT check aspirationally.
- **Estimate**: Relative (e.g., 2h, 1d). Helps with planning / velocity later.
- **Additional Context**: Links, screenshots, logs.

Best Practices:
- Split large features into smaller Issues (API, DTOs, UI separately) when possible.
- Keep acceptance criteria stable; if they change mid-development, update the Issue explicitly.

---
## 4. Pull Request Template Usage
The PR template enforces review discipline.

Section Guidance:
- **Summary**: One or two sentences—what & why.
- **Type of Change**: Check exactly one primary type; multiple types suggest splitting.
- **Layer(s) Affected**: Must match the actual diff; reviewers verify boundary integrity.
- **Checklist**: Do not check items you have not verified locally. Especially:
  - Async + `CancellationToken` propagation.
  - DTO usage at boundaries (no EF entities leaving Application/Infrastructure).
  - No EF attributes on Domain entities.
  - Tests updated or added (if logic changed or added).
- **Implementation Notes**: Document trade‑offs (e.g., chose in-memory cache placeholder; will swap to Redis later). This reduces tribal knowledge.
- **Testing**: List test classes or manual sequences executed.
- **Related Issues**: Use `Closes #123` to auto-close.
- **Follow-Up Tasks**: Use for deferred items to avoid scope creep.

When to Draft vs Ready:
- Open as **Draft** if architectural direction still under discussion.
- Mark **Ready for Review** only after checklist passes locally.

---
## 5. Definition of Done
An Issue / PR is Done when:
1. All Acceptance Criteria satisfied.
2. All PR checklist items verified.
3. Plan (`doc/Plan.md`) updated if the roadmap changes or a task is completed/added.
4. Build succeeds locally (and CI once configured).
5. No console or build warnings introduced (unless justified & documented).
6. Seed changes (if any) are idempotent and necessary.
7. No architecture boundary violations introduced.

---
## 6. Labels & Status Suggestions (Optional)
Suggested labels (create in repo settings):
- `type:feature`, `type:bug`, `type:refactor`, `type:docs`
- `layer:domain`, `layer:application`, `layer:infrastructure`, `layer:presentation`
- `status:blocked`, `status:in-progress`, `status:needs-review`
- `priority:high`, `priority:normal`, `priority:low`

Usage:
- Apply `status:in-progress` when branch created.
- Remove `status:in-progress` + add `status:needs-review` on PR open.
- Use `status:blocked` with a comment referencing dependency.

---
## 7. Architectural Boundaries Recap
- Domain: Pure entities/value objects, no EF, no logging, no external libs.
- Application: Use cases, DTOs, mapping, interfaces for outward dependencies, no EF specifics.
- Infrastructure: EF Core, repository implementations, external API clients, Identity.
- Presentation: API controllers, UI projects (Blazor/Mobile), mapping to/from DTOs.
- Data crosses Application → Presentation boundary only via DTOs.
- Time, external services, and persistence accessed *only* via interfaces defined inward.

Violation Examples:
- Returning `Flight` (entity) directly from API → NOT allowed.
- Injecting `DbContext` into a Blazor component → NOT allowed.
- Using Radzen types in Application layer → NOT allowed.

---
## 8. Common Pitfalls to Avoid
| Pitfall | Avoid By |
|---------|----------|
| Adding EF attributes to Domain | Use Fluent API in Infrastructure |
| Passing entities to UI | Map to DTOs in Application |
| Forgetting CancellationToken | Always last param with default = default |
| Bloated service doing queries + mapping + validation | Separate concerns (repo + service + mapper) |
| Hard-coded DateTime.UtcNow in Domain | Introduce IClock abstraction (later) |
| Unbounded list endpoints | Add pagination / filtering strategy |
| Silent seed drift | Keep SeedData deterministic & documented |

---
## FAQ
**Q: Can I add a generic repository now?**  
A: Not unless explicitly needed; focused repositories are clearer for now.

**Q: Where do provider adapters (OpenSky, FR24) live?**  
A: Infrastructure (implement interface defined in Application).

**Q: Can UI projects reference Infrastructure?**  
A: Prefer not. UI → Application; Infrastructure injected via DI in API host. (For Blazor WASM, call API instead of referencing Infrastructure.)

---
## Next Improvements to This Guide
- Add diagram references once architecture diagram is created.
- Add sample PR diff illustrating good separation.
- Add guidance for versioning & release notes when first release nears.

---
Happy flying & clean coding! ✈️
