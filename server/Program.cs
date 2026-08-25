using Microsoft.EntityFrameworkCore;
using Npgsql;
using RRVMS.Api.Data;
using RRVMS.Api.Middleware;
using RRVMS.Api.Services;

LoadLocalEnvironmentFile();

// MIGRATION MODE: If --migrate argument is provided, run migrations and exit
if (args.Contains("--migrate"))
{
    await RunMigrationsAndExit();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Hosting platforms such as Render inject a PORT environment variable; bind to it on all interfaces.
var runtimePort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(runtimePort) && int.TryParse(runtimePort, out var parsedPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{parsedPort}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseUrl = builder.Configuration["DATABASE_URL"];
var connectionString = string.IsNullOrWhiteSpace(databaseUrl)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        ? NormalizeDatabaseUrl(databaseUrl)
        : databaseUrl;
connectionString = NormalizeNpgsqlConnectionString(connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DATABASE_URL is not configured. Set it in server/.env, as an environment variable, or use .NET user secrets.");
}

builder.Services.AddDbContext<RrvmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IVisitorRequestService, VisitorRequestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, MockCurrentUserService>();

var corsOrigins = new List<string>();
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    foreach (var candidate in frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var origin)) corsOrigins.Add(origin.GetLeftPart(UriPartial.Authority));
    }
}

if (builder.Environment.IsDevelopment())
{
    corsOrigins.Add("http://localhost:5173");
    corsOrigins.Add("http://localhost:5174");
}

if (corsOrigins.Count == 0)
{
    throw new InvalidOperationException(
        "FRONTEND_URL is not configured. Set it to the deployed frontend origin (e.g. https://your-app.vercel.app); multiple origins may be comma-separated.");
}

builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy.WithOrigins(corsOrigins.ToArray()).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Seed development / demo data idempotently
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RrvmsDbContext>();
    await DbSeeder.SeedAsync(dbContext, app.Environment);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Client");
app.MapControllers();

app.Run();

// MIGRATION EXECUTION MODE
async Task RunMigrationsAndExit()
{
    try
    {
        LoadLocalEnvironmentFile();
        
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var connectionString = string.IsNullOrWhiteSpace(databaseUrl)
            ? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            : databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                ? NormalizeDatabaseUrl(databaseUrl)
                : databaseUrl;
        connectionString = NormalizeNpgsqlConnectionString(connectionString);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DATABASE_URL is not configured.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<RrvmsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        using var context = new RrvmsDbContext(optionsBuilder.Options);
        Console.WriteLine("Connecting to database...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migrations completed successfully.");
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Migration failed: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
}

static string NormalizeDatabaseUrl(string databaseUrl)
{
    var value = databaseUrl.Trim();
    if (value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)) value = value[13..];
    else if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)) value = value[11..];
    else
    {
        throw new InvalidOperationException("DATABASE_URL must be a valid postgresql:// connection URL.");
    }

    var authorityEnd = value.IndexOfAny(['/','?','#']);
    var authority = authorityEnd >= 0 ? value[..authorityEnd] : value;
    var database = authorityEnd >= 0 ? value[(authorityEnd + 1)..].TrimStart('/').Split(['?', '#'], 2)[0] : string.Empty;
    var separator = authority.LastIndexOf('@');
    if (separator <= 0)
    {
        throw new InvalidOperationException("DATABASE_URL must include a username and password.");
    }

    var userInfo = authority[..separator].Split(':', 2);
    if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(userInfo[0]) || string.IsNullOrWhiteSpace(userInfo[1]))
    {
        throw new InvalidOperationException("DATABASE_URL must include a username and password.");
    }

    var hostPort = authority[(separator + 1)..];
    var portSeparator = hostPort.LastIndexOf(':');
    var host = portSeparator > 0 ? hostPort[..portSeparator] : hostPort;
    var port = portSeparator > 0 && int.TryParse(hostPort[(portSeparator + 1)..], out var parsedPort) ? parsedPort : 5432;
    var connectionBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = database,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = SslMode.Require,
    };

    if (string.IsNullOrWhiteSpace(connectionBuilder.Database))
    {
        throw new InvalidOperationException("DATABASE_URL must include a database name.");
    }

    return connectionBuilder.ConnectionString;
}

static string? NormalizeNpgsqlConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
    return new NpgsqlConnectionStringBuilder(connectionString).ConnectionString;
}

static void LoadLocalEnvironmentFile()
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath)) return;

    foreach (var line in File.ReadLines(envPath))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
        var separator = trimmed.IndexOf('=');
        if (separator <= 0) continue;

        var key = trimmed[..separator].Trim();
        var value = trimmed[(separator + 1)..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
