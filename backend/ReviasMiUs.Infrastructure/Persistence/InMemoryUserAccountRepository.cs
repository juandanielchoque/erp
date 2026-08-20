using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryUserAccountRepository(InMemoryErpStore store) : IUserAccountRepository
{
    public IReadOnlyCollection<UserAccount> List() => store.Users.ToArray();
    public UserAccount? GetById(Guid id) => store.Users.FirstOrDefault(user => user.Id == id);
    public UserAccount? GetByEmail(string email) => store.Users.FirstOrDefault(user =>
        string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
    public void Add(UserAccount user) => store.Users.Add(user);

    public void Update(UserAccount user)
    {
        var index = store.Users.FindIndex(item => item.Id == user.Id);
        if (index >= 0)
        {
            store.Users[index] = user;
        }
    }
    public void Delete(Guid id) => store.Users.RemoveAll(item => item.Id == id);
}
