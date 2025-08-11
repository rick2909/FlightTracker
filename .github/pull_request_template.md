## Summary
Describe the change clearly. What problem does it solve?

## Type of Change
- [ ] Feature
- [ ] Bug fix
- [ ] Refactor (no functional change)
- [ ] Docs / Chore
- [ ] Tests only

## Layer(s) Affected
- [ ] Domain
- [ ] Application
- [ ] Infrastructure
- [ ] Presentation (API / Web / Desktop / Mobile)

## Checklist
- [ ] Follows Clean Architecture boundaries (no inward dependency violations)
- [ ] No EF Core attributes added to Domain entities
- [ ] Async methods end with `Async` and pass `CancellationToken`
- [ ] Added/updated DTOs (if crossing Application -> Presentation)
- [ ] Updated SeedData only if necessary (and idempotent)
- [ ] Added/updated tests (unit / integration as appropriate)
- [ ] Updated documentation / Plan.md if scope impacts roadmap
- [ ] No unrelated formatting noise in diff

## Implementation Notes
Any noteworthy design decisions, trade-offs, or follow-up work deferred.

## Testing
Describe how you validated the change. Include commands, test IDs, or manual steps.

## Screenshots / Logs (optional)
Attach or describe if UI/log output changed.

## Related Issues
Closes # (issue)
Refs # (optional)

## Follow-Up Tasks (optional)
List items intentionally deferred.
