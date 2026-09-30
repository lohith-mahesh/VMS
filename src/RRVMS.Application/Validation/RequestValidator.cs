using RRVMS.Application.Common;
using RRVMS.Application.Contracts;
using System.ComponentModel.DataAnnotations;

namespace RRVMS.Application.Validation;

public static class RequestValidator
{
    private static readonly HashSet<string> AllowedCorrectionFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "firstName", "middleName", "lastName", "citizenship", "designation", "companyName",
        "companyAddress", "officeCity", "officeCountry", "phoneCountry", "phoneDialCode", "telephone", "email",
        "idType", "otherIdType", "assets", "dpsDocument", "siteCode", "purposeType", "purpose",
        "areasToVisit", "mainHost", "escortingHost"
    };

    public static void Validate(CreateVisitorRequestCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.SiteCode)) errors["siteCode"] = ["A site is required."];
        if (string.IsNullOrWhiteSpace(command.Purpose)) errors["purpose"] = ["A visit purpose is required."];
        if (string.IsNullOrWhiteSpace(command.MainHostName)) errors["mainHostName"] = ["A main host is required."];
        if (command.NumberOfVisitors is < 1 or > 20) errors["numberOfVisitors"] = ["The visitor count must be between 1 and 20."];
        AddLengthError(errors, "siteCode", command.SiteCode, 50);
        AddLengthError(errors, "purpose", command.Purpose, 2000);
        AddLengthError(errors, "areasToVisit", command.AreasToVisit, 1000);
        AddLengthError(errors, "mainHostObjectId", command.MainHostObjectId, 64);
        AddLengthError(errors, "mainHostName", command.MainHostName, 200);
        AddLengthError(errors, "hostDepartment", command.HostDepartment, 200);
        AddLengthError(errors, "escortingHostObjectId", command.EscortingHostObjectId, 64);
        AddLengthError(errors, "escortingHostName", command.EscortingHostName, 200);
        ThrowIfAny(errors);
    }

    public static void Validate(RequestCorrectionCommand command)
    {
        var fields = command.Fields ?? [];
        var invalidFields = fields.Where(field => !AllowedCorrectionFields.Contains(field)).ToArray();
        var errors = new Dictionary<string, string[]>();
        if (fields.Count == 0 || invalidFields.Length > 0) errors["fields"] = ["Select valid visitor fields."];
        if (string.IsNullOrWhiteSpace(command.Instructions)) errors["instructions"] = ["Instructions are required."];
        AddLengthError(errors, "instructions", command.Instructions, 2000);
        ThrowIfAny(errors);
    }

    public static void Validate(SubmitVisitorCommand command)
    {
        var assets = command.Assets ?? [];
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.FirstName)) errors["firstName"] = ["A first name is required."];
        if (string.IsNullOrWhiteSpace(command.LastName)) errors["lastName"] = ["A last name is required."];
        if (string.IsNullOrWhiteSpace(command.Citizenship)) errors["citizenship"] = ["Citizenship is required."];
        if (string.IsNullOrWhiteSpace(command.CompanyName)) errors["companyName"] = ["A company is required."];
        if (!string.IsNullOrWhiteSpace(command.Email) && !new EmailAddressAttribute().IsValid(command.Email)) errors["email"] = ["Enter a valid email address."];
        if (assets.Count > 50) errors["assets"] = ["A visitor cannot declare more than 50 assets."];
        AddLengthError(errors, "firstName", command.FirstName, 100);
        AddLengthError(errors, "middleName", command.MiddleName, 100);
        AddLengthError(errors, "lastName", command.LastName, 100);
        AddLengthError(errors, "citizenship", command.Citizenship, 100);
        AddLengthError(errors, "designation", command.Designation, 200);
        AddLengthError(errors, "companyName", command.CompanyName, 250);
        AddLengthError(errors, "companyAddress", command.CompanyAddress, 1000);
        AddLengthError(errors, "officeCity", command.OfficeCity, 120);
        AddLengthError(errors, "officeCountry", command.OfficeCountry, 120);
        AddLengthError(errors, "phoneCountry", command.PhoneCountry, 120);
        AddLengthError(errors, "phoneDialCode", command.PhoneDialCode, 10);
        AddLengthError(errors, "telephone", command.Telephone, 30);
        AddLengthError(errors, "email", command.Email, 320);
        AddLengthError(errors, "idType", command.IdType, 120);
        AddLengthError(errors, "otherIdType", command.OtherIdType, 200);
        if (assets.Any(asset => (asset.AssetType?.Length ?? 0) > 100 || (asset.Description?.Length ?? 0) > 500 || (asset.SerialNumber?.Length ?? 0) > 200))
        {
            errors["assets"] = ["Asset types, descriptions, and serial numbers exceed the allowed length."];
        }

        ThrowIfAny(errors);
    }

    public static void Validate(ReviseRequestCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.SiteCode)) errors["siteCode"] = ["A site is required."];
        if (string.IsNullOrWhiteSpace(command.Purpose)) errors["purpose"] = ["A purpose is required."];
        if (string.IsNullOrWhiteSpace(command.MainHostObjectId) || string.IsNullOrWhiteSpace(command.MainHostName)) errors["mainHost"] = ["A main host is required."];
        AddLengthError(errors, "siteCode", command.SiteCode, 50);
        AddLengthError(errors, "purpose", command.Purpose, 2000);
        AddLengthError(errors, "areasToVisit", command.AreasToVisit, 1000);
        AddLengthError(errors, "mainHostObjectId", command.MainHostObjectId, 64);
        AddLengthError(errors, "mainHostName", command.MainHostName, 200);
        AddLengthError(errors, "hostDepartment", command.HostDepartment, 200);
        AddLengthError(errors, "escortingHostObjectId", command.EscortingHostObjectId, 64);
        AddLengthError(errors, "escortingHostName", command.EscortingHostName, 200);
        ThrowIfAny(errors);
    }

    public static void Validate(ReviewVisitorsCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.VisitorIds is null || command.VisitorIds.Count == 0) errors["visitorIds"] = ["Select at least one visitor."];
        AddLengthError(errors, "comments", command.Comments, 2000);
        ThrowIfAny(errors);
    }

    public static void Validate(RescheduleCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Reason)) errors["reason"] = ["A reason is required."];
        AddLengthError(errors, "reason", command.Reason, 2000);
        ThrowIfAny(errors);
    }

    public static void Validate(CancelRequestCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Reason)) errors["reason"] = ["A reason is required."];
        AddLengthError(errors, "reason", command.Reason, 2000);
        ThrowIfAny(errors);
    }

    public static void Validate(VerifyEntryCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (!command.Approved && string.IsNullOrWhiteSpace(command.Remarks)) errors["remarks"] = ["Remarks are required when entry is rejected."];
        AddLengthError(errors, "remarks", command.Remarks, 2000);
        ThrowIfAny(errors);
    }

    public static void Validate(CheckInCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.BadgeId)) errors["badgeId"] = ["A badge ID is required."];
        AddLengthError(errors, "badgeId", command.BadgeId, 100);
        ThrowIfAny(errors);
    }

    public static void ValidateRowVersion(byte[] rowVersion)
    {
        if (rowVersion.Length != 8)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["rowVersion"] = ["The request version is missing or invalid. Refresh the request and try again."]
            });
        }
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void AddLengthError(Dictionary<string, string[]> errors, string field, string? value, int maximum)
    {
        if (value?.Length > maximum)
        {
            errors[field] = [$"The value cannot exceed {maximum} characters."];
        }
    }
}
