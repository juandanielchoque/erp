namespace ReviasMiUs.Application.Dtos;

public sealed record LeadDto(
    Guid Id,
    string Name,
    string Company,
    string Email,
    string? Phone,
    string Source,
    string Stage,
    int Score,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc);

public sealed record CreateLeadRequest(
    string Name,
    string Company,
    string Email,
    string? Phone,
    string Source,
    int Score);

public sealed record UpdateLeadStageRequest(string Stage);
public sealed record UpdateLeadRequest(string Name, string Company, string Email, string? Phone, string Source, int Score, string Stage);

public sealed record CrmDashboardDto(
    int TotalLeads,
    int ActiveLeads,
    int WonLeads,
    int LostLeads,
    int HighPriorityLeads,
    IReadOnlyCollection<string> Sources);
