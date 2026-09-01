# Visitor Management System

Visitor management system foundation for host, export-control, and reception workflows.

## Run the client

```powershell
cd client
npm install
npm run dev
```

Set `VITE_API_BASE_URL` in `client/.env` when the API is not running at the default `http://localhost:5000`.

## Run the API

Requires the .NET 10 SDK and a Supabase PostgreSQL connection string. The API is the only component that connects to PostgreSQL.

Configure the connection without committing credentials. PowerShell environment configuration for the current session:

```powershell
$env:DATABASE_URL = "postgresql://user:password@host:5432/database"
```

Alternatively, use .NET user secrets from the `server` directory:

```powershell
dotnet user-secrets init
dotnet user-secrets set "DATABASE_URL" "postgresql://user:password@host:5432/database"
```

Do not commit the password. If using a URI-style connection string, percent-encode special characters in the password.

```powershell
cd server
dotnet restore
dotnet run
```

The health endpoint is `GET /api/health`. It executes `SELECT 1` through EF Core and returns HTTP 503 when PostgreSQL is unavailable.

Run migrations after adding a model:

```powershell
cd server
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Verify the live database connection:

```powershell
Invoke-RestMethod http://localhost:5000/api/health
```

An available database returns `status: ok`, `service: RRVMS API`, and `database: connected`.
