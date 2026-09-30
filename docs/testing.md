# Verification strategy

## Automated suites

- Domain tests cover auto-approval, partial approval, immutable decisions, DPS replacement, arrival classification, check-in locks, and retention.
- Application tests cover input and concurrency-token validation.
- Architecture tests prevent dependency inversion regressions.
- API integration tests verify anonymous health and development identity behavior.
- React tests verify accessible UI primitives and status formatting.
- Strict TypeScript and warnings-as-errors C# builds are mandatory.
- CI checks known vulnerable direct and transitive dependencies and compiles the Bicep template.

## Required pre-production suites

1. Run all automated tests against the pinned build output.
2. Run migrations against a restored production-sized Azure SQL copy.
3. Test two-user concurrency on request, screening, and Reception actions.
4. Verify all three Entra roles and negative cross-role cases.
5. Verify Host upload, Host download denial, Reception metadata/file denial, Export Control download, and access-event persistence.
6. Test internal, external, partially approved, correction, reschedule, cancellation, multi-day, no-show, early, on-time, and late paths.
7. Run WCAG 2.2 AA automated and manual keyboard/screen-reader testing.
8. Run authenticated load tests with realistic request and report volumes.
9. Run SAST, dependency, container, infrastructure, and dynamic security scans.
10. Restore SQL and Blob backups in an isolated subscription and prove the recovery objectives.

Production acceptance must record the build identifier, database migration identifier, test evidence, approvers, known residual risks, and rollback version.
