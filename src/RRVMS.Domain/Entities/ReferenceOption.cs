using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class ReferenceOption : Entity
{
    private ReferenceOption()
    {
    }

    public ReferenceOption(string category, string code, string displayName, int sortOrder)
    {
        Category = category;
        Code = code;
        DisplayName = displayName;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Category { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Rename(string displayName) => DisplayName = displayName.Trim();
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
