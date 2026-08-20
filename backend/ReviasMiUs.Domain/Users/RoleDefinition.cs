using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Users;

public sealed class RoleDefinition : Entity
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsSystem { get; }
    public IReadOnlyCollection<string> Permissions => _permissions.OrderBy(item => item).ToArray();

    public RoleDefinition(string code, string name, string description, IEnumerable<string> permissions, bool isSystem = false)
    {
        Code = ValidateCode(code);
        Name = ValidateName(name);
        Description = description?.Trim() ?? string.Empty;
        IsSystem = isSystem;
        ReplacePermissions(permissions);
    }

    public void Update(string name, string description, IEnumerable<string> permissions)
    {
        Name = ValidateName(name);
        Description = description?.Trim() ?? string.Empty;
        ReplacePermissions(permissions);
    }

    private void ReplacePermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        foreach (var permission in permissions.Where(item => !string.IsNullOrWhiteSpace(item)))
            _permissions.Add(permission.Trim());
    }

    private static string ValidateCode(string code)
    {
        var value = code?.Trim() ?? string.Empty;
        if (value.Length is < 3 or > 40 || value.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new DomainException("Role code must contain 3 to 40 letters, numbers, hyphens or underscores.");
        return value;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name is required.");
        return name.Trim();
    }
}
