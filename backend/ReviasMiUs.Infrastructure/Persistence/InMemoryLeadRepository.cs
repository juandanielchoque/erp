using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Crm;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryLeadRepository(InMemoryErpStore store) : ILeadRepository
{
    public IReadOnlyCollection<Lead> List() => store.Leads.ToArray();

    public Lead? GetById(Guid id) => store.Leads.FirstOrDefault(lead => lead.Id == id);

    public void Add(Lead lead) => store.Leads.Add(lead);

    public void Update(Lead lead)
    {
        var index = store.Leads.FindIndex(item => item.Id == lead.Id);
        if (index >= 0)
        {
            store.Leads[index] = lead;
        }
    }
    public void Delete(Guid id) => store.Leads.RemoveAll(item => item.Id == id);
}
