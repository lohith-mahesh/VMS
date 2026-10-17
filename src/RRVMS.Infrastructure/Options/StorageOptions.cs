namespace RRVMS.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "DocumentStorage";
    public string Provider { get; init; } = "File";
    public string ServiceUri { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "dps-documents";
    public string LocalPath { get; init; } = "App_Data/documents";
}

public sealed class GraphOptions
{
    public const string SectionName = "MicrosoftGraph";
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://graph.microsoft.com/v1.0";
    public string SenderMailbox { get; init; } = string.Empty;
}
