using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RRVMS.Application.Abstractions;
using RRVMS.Infrastructure.Background;
using RRVMS.Infrastructure.Documents;
using RRVMS.Infrastructure.Graph;
using RRVMS.Infrastructure.Options;
using RRVMS.Infrastructure.Persistence;
using RRVMS.Infrastructure.Services;

namespace RRVMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var connectionString = configuration.GetConnectionString("Rrvms")
            ?? throw new InvalidOperationException("ConnectionStrings:Rrvms is required.");
        services.AddDbContext<RrvmsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                sql.CommandTimeout(60);
            }));
        services.AddScoped<IVisitorRequestRepository, VisitorRequestRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(_ => ResolveTimeZone(configuration["Application:TimeZoneId"] ?? "UTC"));
        ConfigureStorage(services, configuration, isDevelopment);
        ConfigureGraph(services, configuration);
        services.AddHostedService<RetentionWorker>();
        return services;
    }

    private static void ConfigureStorage(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        services.AddSingleton(options);
        if (string.Equals(options.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<TokenCredential, DefaultAzureCredential>();
            services.AddSingleton(provider =>
            {
                BlobServiceClient service;
                if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    service = new BlobServiceClient(options.ConnectionString);
                }
                else if (Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out var serviceUri))
                {
                    service = new BlobServiceClient(serviceUri, provider.GetRequiredService<TokenCredential>());
                }
                else
                {
                    throw new InvalidOperationException("DocumentStorage:ServiceUri is required for Azure Blob Storage.");
                }

                return service.GetBlobContainerClient(options.ContainerName);
            });
            services.AddScoped<IDocumentStorage, AzureBlobDocumentStorage>();
            return;
        }

        if (!isDevelopment)
        {
            throw new InvalidOperationException("AzureBlob document storage is required outside Development.");
        }

        services.AddSingleton<IDocumentStorage, FileDocumentStorage>();
    }

    private static void ConfigureGraph(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(GraphOptions.SectionName).Get<GraphOptions>() ?? new GraphOptions();
        services.AddSingleton(options);
        if (!options.Enabled)
        {
            services.AddSingleton<IEmployeeDirectory, DisabledEmployeeDirectory>();
            services.AddSingleton<IEmailSender, DisabledEmailSender>();
            return;
        }

        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddTransient<GraphAccessTokenHandler>();
        services.AddHttpClient<IEmployeeDirectory, GraphEmployeeDirectory>().AddHttpMessageHandler<GraphAccessTokenHandler>();
        services.AddHttpClient<IEmailSender, GraphEmailSender>().AddHttpMessageHandler<GraphAccessTokenHandler>();
        services.AddHostedService<OutboxWorker>();
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException($"Application:TimeZoneId '{timeZoneId}' is invalid.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException($"Application:TimeZoneId '{timeZoneId}' is invalid.", exception);
        }
    }
}
