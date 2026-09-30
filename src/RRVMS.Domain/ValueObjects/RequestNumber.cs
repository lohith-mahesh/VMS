using System.Text.RegularExpressions;
using RRVMS.Domain.Common;

namespace RRVMS.Domain.ValueObjects;

public sealed partial record RequestNumber
{
    private RequestNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RequestNumber Create(int year, int month, int sequence)
    {
        if (year < 2000 || month is < 1 or > 12 || sequence < 1)
        {
            throw new DomainException("The request number inputs are invalid.");
        }

        return new RequestNumber($"V-{month:00}-{year % 100:00}-{sequence:0000}");
    }

    public static RequestNumber Parse(string value)
    {
        if (!RequestNumberPattern().IsMatch(value))
        {
            throw new DomainException("The request number format is invalid.");
        }

        return new RequestNumber(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^V-(0[1-9]|1[0-2])-\\d{2}-\\d{4,}$", RegexOptions.CultureInvariant)]
    private static partial Regex RequestNumberPattern();
}

