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

    public static RequestNumber Create(int year, int sequence)
    {
        if (year < 2000 || sequence < 1)
        {
            throw new DomainException("The request number inputs are invalid.");
        }

        return new RequestNumber($"V-{year}-{sequence}");
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

    [GeneratedRegex("^V-\\d{4}-[1-9]\\d*$", RegexOptions.CultureInvariant)]
    private static partial Regex RequestNumberPattern();
}

