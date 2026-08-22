using Microsoft.EntityFrameworkCore;
using Npgsql;
using RRVMS.Api.Data;
using RRVMS.Api.Middleware;
using RRVMS.Api.Services;

LoadLocalEnvironmentFile();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var databaseUrl = builder.Configuration["DATABASE_URL"];
var connectionString = string.IsNullOrWhiteSpace(databaseUrl)
	? builder.Configuration.GetConnectionString("DefaultConnection")
	: NormalizeDatabaseUrl(databaseUrl);
if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException(
		"DATABASE_URL is not configured. Set it in server/.env, as an environment variable, or use .NET user secrets.");
}

builder.Services.AddDbContext<RrvmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IVisitorRequestService, VisitorRequestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, MockCurrentUserService>();
builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Client");
app.MapControllers();
app.Run();

static string NormalizeDatabaseUrl(string databaseUrl)
{
	if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres"))
	{
		throw new InvalidOperationException("DATABASE_URL must be a valid postgresql:// connection URL.");
	}

	var userInfo = uri.UserInfo.Split(':', 2);
	if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(userInfo[0]) || string.IsNullOrWhiteSpace(userInfo[1]))
	{
		throw new InvalidOperationException("DATABASE_URL must include a username and password.");
	}

	var connectionBuilder = new NpgsqlConnectionStringBuilder
	{
		Host = uri.Host,
		Port = uri.Port > 0 ? uri.Port : 5432,
		Database = uri.AbsolutePath.Trim('/'),
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
