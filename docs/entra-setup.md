# Microsoft Entra ID setup

## API registration

Create a single-tenant app registration for the RRVMS API.

1. Expose an API scope named `access_as_user`.
2. Define app roles with exact values `HostRequester`, `ExportControl`, and `Reception`.
3. Set each role's allowed member type to Users/Groups.
4. Assign each person or security group exactly one RRVMS role.
5. Set the API `ClientId`, tenant, and audience in App Service configuration.

The API rejects authenticated users with zero or multiple recognized RRVMS roles. Role assignment is therefore explicit and unambiguous.

## SPA registration

Create a single-tenant SPA app registration.

1. Add the deployed Static Web App URL as a SPA redirect URI.
2. Add `http://localhost:5173` only to the non-production registration.
3. Grant delegated permission to the API `access_as_user` scope.
4. Configure `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, and `VITE_API_SCOPE` at frontend build time.

The SPA uses authorization code flow with PKCE. A SPA is a public client and must not be given a client secret.

## Managed identity and Microsoft Graph

The API App Service uses its system-assigned managed identity. When Graph integration is approved, grant that enterprise application:

- `User.Read.All` application permission for employee directory search.
- `Mail.Send` application permission for the approved sender mailbox.

Apply an Exchange Online application access policy so the managed identity can send only from the approved mailbox. Set `MicrosoftGraph__Enabled=true` only after permissions, the mailbox, and the access policy are verified. Until then, directory search returns no results and notification messages remain in the outbox.

## Azure SQL identity

Deploy the SQL logical server with Entra-only authentication. Connect as the configured Entra administrator, run the schema migration, replace the SQLCMD variable in `database/bootstrap/001_app_identity.sql` with the API App Service name, and run the bootstrap script in the `rrvms` database.

The API connection string uses `Authentication=Active Directory Default`; do not add a SQL password to App Service settings.
