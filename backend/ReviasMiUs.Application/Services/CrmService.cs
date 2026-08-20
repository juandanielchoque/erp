using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Crm;

namespace ReviasMiUs.Application.Services;

public sealed class CrmService(ILeadRepository leads)
{
    public LeadDto Create(CreateLeadRequest request)
    {
        var lead = new Lead(
            request.Name,
            request.Company,
            request.Email,
            request.Phone,
            request.Source,
            request.Score);

        leads.Add(lead);
        return Map(lead);
    }

    public IReadOnlyCollection<LeadDto> List() =>
        leads.List()
            .OrderByDescending(lead => lead.Score)
            .ThenBy(lead => lead.Name)
            .Select(Map)
            .ToArray();

    public IReadOnlyCollection<LeadDto> Search(string term)
    {
        var normalized = term.Trim();
        return leads.List()
            .Where(lead =>
                lead.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                lead.Company.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                lead.Email.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(lead => lead.Score)
            .ThenBy(lead => lead.Name)
            .Select(Map)
            .ToArray();
    }

    public LeadDto UpdateStage(Guid id, UpdateLeadStageRequest request)
    {
        var lead = leads.GetById(id) ?? throw new DomainException("Lead not found.");
        if (!Enum.TryParse<LeadStage>(request.Stage, true, out var stage))
        {
            throw new DomainException("Invalid lead stage.");
        }

        lead.UpdateStage(stage);
        leads.Update(lead);
        return Map(lead);
    }

    public LeadDto Update(Guid id, UpdateLeadRequest request)
    {
        var lead = leads.GetById(id) ?? throw new DomainException("Lead not found.");
        if (!Enum.TryParse<LeadStage>(request.Stage, true, out var stage))
            throw new DomainException("Invalid lead stage.");
        lead.UpdateDetails(request.Name, request.Company, request.Email, request.Phone, request.Source, request.Score);
        lead.UpdateStage(stage);
        leads.Update(lead);
        return Map(lead);
    }

    public void Delete(Guid id)
    {
        _ = leads.GetById(id) ?? throw new DomainException("Lead not found.");
        leads.Delete(id);
    }

    public CrmDashboardDto GetDashboard()
    {
        var allLeads = leads.List();
        var activeLeads = allLeads.Where(lead => lead.IsActive()).ToArray();
        var wonLeads = allLeads.Count(lead => lead.Stage == LeadStage.Won);
        var lostLeads = allLeads.Count(lead => lead.Stage == LeadStage.Lost);
        var highPriorityLeads = activeLeads.Count(lead => lead.Score >= 80);
        var sources = allLeads
            .Select(lead => lead.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(source => source)
            .ToArray();

        return new CrmDashboardDto(
            allLeads.Count,
            activeLeads.Length,
            wonLeads,
            lostLeads,
            highPriorityLeads,
            sources);
    }

    private static LeadDto Map(Lead lead) =>
        new(
            lead.Id,
            lead.Name,
            lead.Company,
            lead.Email,
            lead.Phone,
            lead.Source,
            lead.Stage.ToString(),
            lead.Score,
            lead.CreatedAtUtc,
            lead.LastActivityAtUtc);
}
