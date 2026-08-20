using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Customers;

public sealed class Customer : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Customer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Customer email is required.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdateDetails(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            throw new DomainException("Customer name and email are required.");
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}
