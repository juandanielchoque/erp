using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryRoleRepository(InMemoryErpStore store) : IRoleRepository
{
    public IReadOnlyCollection<RoleDefinition> List() => store.Roles.ToArray();
    public RoleDefinition? GetByCode(string code) => store.Roles.FirstOrDefault(role => string.Equals(role.Code, code, StringComparison.OrdinalIgnoreCase));
    public void Add(RoleDefinition role) => store.Roles.Add(role);
    public void Update(RoleDefinition role)
    {
        var index = store.Roles.FindIndex(item => item.Id == role.Id);
        if (index >= 0) store.Roles[index] = role;
    }
    public void Delete(Guid id) => store.Roles.RemoveAll(role => role.Id == id);
}
