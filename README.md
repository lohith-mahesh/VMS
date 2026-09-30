# RRVMS

RRVMS is a production-oriented visitor request, Export Control screening, and Reception processing system. It replaces the single-file Python prototype with a React single-page application, an ASP.NET Core API, Azure SQL, private Azure Blob storage, Microsoft Entra ID authentication, Microsoft Graph integration, audit history, and automated retention.

## Repository structure

```text
RRVMS/
├── apps/web/                         React, TypeScript, Vite and MSAL
│   ├── src/app/                      Routing, identity context and shell
│   ├── src/auth/                     Microsoft Entra ID and development auth
│   ├── src/components/               Shared UI components
│   ├── src/features/                 Dashboard, requests, visitors and reports
│   ├── src/lib/                      API client, types and formatting
│   └── src/styles/                   Responsive design system
├── src/RRVMS.Domain/                 Workflow rules and entities
├── src/RRVMS.Application/            Use cases, validation and contracts
├── src/RRVMS.Infrastructure/         SQL, Blob, Graph and background workers
├── src/RRVMS.Api/                    Authenticated HTTP API
├── tests/                             Domain, application, architecture and API tests
├── database/                          Idempotent SQL migrations and bootstrap scripts
├── infra/bicep/                       Azure infrastructure as code
├── docs/                              Architecture, security and operations
├── .github/workflows/                 Continuous integration
└── docker-compose.yml                 Local SQL, API and web stack
```

## Principal workflows

- A Host creates a request for 1–20 internal or external visitors.
- Each visitor has an independent details form, asset declaration, workflow decision, and daily Reception record.
- Internal visitors auto-approve when all details are complete.
- External visitors require one DPS PDF each and are submitted to Export Control.
- Export Control can decide individually or in batches. Mixed decisions produce a partially approved request and approved visitors continue.
- Export Control can request corrections. Material site, purpose, host, identity, company, asset, or DPS corrections reset the applicable screening state.
- Hosts can reschedule or cancel until any visitor checks in. An external reschedule resets screening.
- Reception verifies identity and assets, issues a unique active badge, checks visitors in and out, and marks no-shows per visit day.
- Arrival is Early more than one hour before the scheduled time, On Time within one hour either side, and Late more than one hour after it.
- Closed records are deleted automatically six years after closure. DPS blobs are deleted with their records.

## Local development

The supported local path uses Docker Desktop or another Docker Compose-compatible runtime:

```bash
export MSSQL_SA_PASSWORD='<strong-local-password>'
docker compose up --build
```

The web application is available on `http://localhost:5173`, and the API is available on `http://localhost:5208`. The local stack uses the development authentication handler. Change the active role from the sidebar.

To run services separately:

```bash
export ConnectionStrings__Rrvms='Server=localhost,1433;Database=Rrvms;User Id=sa;Password=<local-password>;Encrypt=false;TrustServerCertificate=true'
dotnet restore RRVMS.sln
dotnet build RRVMS.sln
dotnet test RRVMS.sln
cd apps/web
npm install
npm run build
npm test
```

Apply the SQL migration before starting the API when Docker Compose is not used:

```bash
sqlcmd -S localhost,1433 -U sa -P '<local-password>' -C -Q "IF DB_ID(N'Rrvms') IS NULL CREATE DATABASE Rrvms"
sqlcmd -S localhost,1433 -U sa -P '<local-password>' -C -d Rrvms -i database/migrations/001_initial.sql
```

## Production configuration

Production rejects file-based document storage and the development authentication scheme. Configure the following settings through App Service configuration:

| Setting | Purpose |
|---|---|
| `AzureAd__TenantId` | Microsoft Entra tenant |
| `AzureAd__ClientId` | API app registration client ID |
| `AzureAd__Audience` | Expected access-token audience |
| `ConnectionStrings__Rrvms` | Azure SQL connection using `Active Directory Default` |
| `DocumentStorage__Provider` | Must be `AzureBlob` |
| `DocumentStorage__ServiceUri` | Blob service URI |
| `DocumentStorage__ContainerName` | Private DPS container |
| `MicrosoftGraph__Enabled` | Enables employee search and notification delivery |
| `MicrosoftGraph__SenderMailbox` | Approved mailbox used by Graph `sendMail` |
| `Cors__AllowedOrigins__0` | Deployed frontend origin |
| `Application__TimeZoneId` | Site business timezone |

The Bicep deployment uses managed identities for Azure SQL and Blob Storage. It does not require application-held database passwords, storage keys, or document-encryption keys. Azure Storage service-side encryption uses Microsoft-managed keys.

## Deployment order

1. Create the Entra app registrations and app roles described in `docs/entra-setup.md`.
2. Replace placeholders in `infra/bicep/main.bicepparam` and deploy `infra/bicep/main.bicep`.
3. Connect to Azure SQL as the configured Entra administrator and run the migration scripts.
4. Run `database/bootstrap/001_app_identity.sql` with the API App Service name substituted.
5. Build and deploy the API and frontend with the environment-specific MSAL values.
6. Complete the production verification checklist in `docs/operations.md`.

## Quality gates

The build treats .NET warnings as errors, enables nullable reference analysis and current analyzers, compiles TypeScript in strict mode, runs domain/application/API/UI tests, checks architecture boundaries, scans dependencies for known vulnerabilities, validates Bicep, and builds both production containers. These controls reduce defect risk substantially; no software process can truthfully guarantee the absence of every possible defect, so production approval should also include organization-specific security, accessibility, performance, recovery, and user-acceptance testing.
