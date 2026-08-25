namespace RRVMS.Api.Data;

public static class CountryCatalog
{
    public static readonly IReadOnlySet<string> Values = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Australia", "Canada", "China", "France", "Germany", "India", "Japan", "Singapore", "United Kingdom", "United States"
    };

    public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && Values.Contains(value.Trim());
}