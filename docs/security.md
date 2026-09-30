# Security model

## Authorization matrix

| Capability | Host / Requester | Export Control | Reception |
|---|---:|---:|---:|
| Create and own request | Yes | No | No |
| Edit visitor details | Own request, allowed states | No | No |
| Upload initial or corrected DPS | Own request | No | No |
| View DPS metadata | Own request | Yes | No |
| Open or download DPS | No | Yes | No |
| Request corrections | No | Yes | No |
| Approve or reject visitors | No | Yes | No |
| Verify identity and assets | No | No | Approved visitors only |
| Check in, check out, no-show | No | No | Approved visitors only |
| Reports | Own data | External visitor data | Reception-visible data |

Route policies are backed by application-layer ownership and state checks. A route policy alone is never treated as sufficient authorization.

## Document handling

- Uploads require a `.pdf` extension, `application/pdf` media type, `%PDF-` signature, and maximum size of 10 MB.
- File names are reduced to their base name and are never used as a storage path.
- Blob names are server generated.
- The storage container has no public access, shared-key access is disabled, and the API uses managed identity.
- Storage is reachable through a private endpoint from the API's VNet integration.
- Blob downloads never expose SAS URLs to the browser.
- Every successful download is committed to `DocumentAccessEvents` and the request audit log before the stream is returned.
- CSV and Excel exports are recorded with actor, role, filters, row count, format, correlation ID, and timestamp.
- Replaced files remain inaccessible but retained until the request's retention date, preserving the audit trail.

## Application controls

- Microsoft Entra JWT validation and exact app-role enforcement
- Strict CORS allowlist
- HTTPS redirection, HSTS, security headers, and API rate limiting
- Central RFC 7807 problem responses without internal exception details
- Parameterized EF Core queries
- Strict length and workflow validation
- SQL row-version conflict detection
- CSV formula-injection neutralization
- Correlation IDs across requests and audit events
- Secrets excluded from source and managed identities used for Azure services

## Retention

Rejected, cancelled, and completed requests receive a `RetainUntil` timestamp six years after closure. The background worker deletes expired DPS blobs and then removes the aggregate in batches. Standalone report-export events and completed outbox records are also removed after six years. Blob deletion is idempotent, so a partially failed batch is safe to retry.

Legal hold is not implemented because it was not part of the supplied rules. Add an explicit hold flag and privileged hold workflow before deployment if corporate policy requires it.
