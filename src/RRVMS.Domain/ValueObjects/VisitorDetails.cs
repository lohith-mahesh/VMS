using RRVMS.Domain.Common;

namespace RRVMS.Domain.ValueObjects;

public sealed record AssetDetails(string AssetType, string Description, string SerialNumber);

public sealed record VisitorDetails(
    string FirstName,
    string MiddleName,
    string LastName,
    string Citizenship,
    string Designation,
    string CompanyName,
    string CompanyAddress,
    string OfficeCity,
    string OfficeCountry,
    string PhoneCountry,
    string PhoneDialCode,
    string Telephone,
    string Email,
    string IdType,
    string OtherIdType,
    IReadOnlyCollection<AssetDetails> Assets)
{
    public string FullName => string.Join(' ', new[] { FirstName, MiddleName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public void Validate()
    {
        var required = new[]
        {
            FirstName, LastName, Citizenship, Designation, CompanyName, CompanyAddress,
            OfficeCity, OfficeCountry, PhoneCountry, PhoneDialCode, Telephone, IdType
        };

        if (required.Any(string.IsNullOrWhiteSpace))
        {
            throw new DomainException("All required visitor fields must be completed.");
        }

        if (FirstName.Trim().Length < 2 || LastName.Trim().Length < 2)
        {
            throw new DomainException("First and last names must contain at least two characters.");
        }

        if (!CountryRules.Countries.ContainsKey(Citizenship.Trim()) || !CountryRules.Countries.ContainsKey(OfficeCountry.Trim()))
        {
            throw new DomainException("Select a valid citizenship and company country.");
        }

        if (!CountryRules.Countries.TryGetValue(PhoneCountry.Trim(), out var phoneRule) || PhoneDialCode.Trim().TrimStart('+') != phoneRule.Dial)
        {
            throw new DomainException("Select a valid phone country and dial code.");
        }

        if (Telephone.Length == 0 || Telephone.Any(character => character is < '0' or > '9') || !phoneRule.Lengths.Contains(Telephone.Length))
        {
            throw new DomainException($"Enter a valid national phone number for {PhoneCountry}.");
        }

        if (IdType is not ("Driver's License" or "Passport" or "Voter ID" or "Aadhaar Card" or "PAN Card" or "Other Government Issued ID"))
        {
            throw new DomainException("Select a valid identity document type.");
        }

        if (string.Equals(IdType, "Other Government Issued ID", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(OtherIdType))
        {
            throw new DomainException("The government-issued identity document type is required.");
        }

        if (Assets.Any(asset => string.IsNullOrWhiteSpace(asset.AssetType) || string.IsNullOrWhiteSpace(asset.SerialNumber)))
        {
            throw new DomainException("Every declared asset requires an asset type and serial number.");
        }

        var serials = Assets.Select(asset => asset.SerialNumber.Trim()).ToArray();
        if (serials.Distinct(StringComparer.OrdinalIgnoreCase).Count() != serials.Length)
        {
            throw new DomainException("Asset serial numbers must be unique for each visitor.");
        }
    }
}

