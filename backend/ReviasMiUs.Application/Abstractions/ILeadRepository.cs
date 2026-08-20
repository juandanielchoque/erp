using ReviasMiUs.Domain.Crm;

namespace ReviasMiUs.Application.Abstractions;

public interface ILeadRepository
{
    IReadOnlyCollection<Lead> List();
    Lead? GetById(Guid id);
    void Add(Lead lead);
    void Update(Lead lead);
    void Delete(Guid id);
}
