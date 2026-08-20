using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Users;

public enum UserRole
{
    Administrator = 0,
    Cashier = 1,
    Sales = 2,
    Warehouse = 3
}

public sealed class UserAccount : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string PasswordHash { get; private set; }

    public UserAccount(string name, string email, string role, string passwordHash = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("User name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainException("A valid user email is required.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = NormalizeRole(role);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        PasswordHash = passwordHash;
    }

    public UserAccount(string name, string email, UserRole role, string passwordHash = "")
        : this(name, email, role.ToString(), passwordHash) { }

    public void ChangeRole(string role) => Role = NormalizeRole(role);
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateProfile(string name, string email, string role)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("A name and valid user email are required.");
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = NormalizeRole(role);
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = passwordHash;
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) throw new DomainException("User role is required.");
        return role.Trim();
    }
}
