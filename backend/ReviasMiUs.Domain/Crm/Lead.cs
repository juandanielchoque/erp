using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Crm;

public enum LeadStage
{
    New = 0,
    Qualified = 1,
    Proposal = 2,
    Won = 3,
    Lost = 4
}

public sealed class Lead : Entity
{
    public string Name { get; private set; }
    public string Company { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string Source { get; private set; }
    public LeadStage Stage { get; private set; }
    public int Score { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastActivityAtUtc { get; private set; }

    public Lead(string name, string company, string email, string? phone, string source, int score)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Lead name is required.");
        }

        if (string.IsNullOrWhiteSpace(company))
        {
            throw new DomainException("Lead company is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Lead email is required.");
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new DomainException("Lead source is required.");
        }

        if (score is < 0 or > 100)
        {
            throw new DomainException("Lead score must be between 0 and 100.");
        }

        Name = name.Trim();
        Company = company.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Source = source.Trim();
        Score = score;
        Stage = LeadStage.New;
        CreatedAtUtc = DateTime.UtcNow;
        LastActivityAtUtc = CreatedAtUtc;
    }

    public void UpdateStage(LeadStage stage)
    {
        Stage = stage;
        LastActivityAtUtc = DateTime.UtcNow;
    }

    public void UpdateScore(int score)
    {
        if (score is < 0 or > 100)
        {
            throw new DomainException("Lead score must be between 0 and 100.");
        }

        Score = score;
        LastActivityAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string company, string email, string? phone, string source, int score)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(company) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(source))
        {
            throw new DomainException("Lead name, company, email and source are required.");
        }
        if (score is < 0 or > 100) throw new DomainException("Lead score must be between 0 and 100.");

        Name = name.Trim();
        Company = company.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Source = source.Trim();
        Score = score;
        LastActivityAtUtc = DateTime.UtcNow;
    }

    public bool IsActive() => Stage is LeadStage.New or LeadStage.Qualified or LeadStage.Proposal;
}
