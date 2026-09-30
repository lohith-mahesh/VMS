# Operations runbook

## Release

1. Confirm CI is green and dependency scans contain no unaccepted high or critical findings.
2. Deploy infrastructure changes with a Bicep what-if review.
3. Back up Azure SQL and verify Blob soft delete.
4. Apply migrations using the Entra deployment identity.
5. Deploy the API, verify `/health/live` and `/health/ready`, then deploy the SPA.
6. Perform one synthetic request for each role.
7. Monitor error rate, dependency failures, latency, outbox backlog, and retention-worker logs.

## Monitoring alerts

- API five-minute failure rate above 2 percent
- API p95 duration above 2 seconds
- Azure SQL CPU or data IO above 80 percent for 15 minutes
- Blob or SQL dependency failures above 1 percent
- Outbox messages with ten attempts
- No successful retention run for 36 hours
- App Service unhealthy instance

## Recovery

Use Azure SQL point-in-time restore and Blob soft delete according to the organization's recovery objectives. Restore into isolated resources first, validate referential integrity and document hashes, then perform a controlled cutover. Never extend a restored record beyond its original `RetainUntil` date without an approved policy exception.

## Graph-disabled operation

`MicrosoftGraph__Enabled=false` is a supported mode. Hosts enter mock or manually verified host information, employee search returns an empty set, and workflow notification records remain queued. Turning Graph on later activates directory search and outbox delivery without a schema change.

## Incident response

For suspected DPS exposure, disable the API App Service, preserve Application Insights and SQL audit evidence, revoke affected sessions and role assignments, identify `DocumentAccessEvents` by document and actor, and follow the organization's privacy and incident-notification procedures.
