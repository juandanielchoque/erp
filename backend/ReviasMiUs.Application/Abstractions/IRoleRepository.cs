using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Application.Abstractions;

public interface IRoleRepository
{
    IReadOnlyCollection<RoleDefinition> List();
    RoleDefinition? GetByCode(string code);
    void Add(RoleDefinition role);
    void Update(RoleDefinition role);
    void Delete(Guid id);
}
