using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Application.Abstractions;

public interface IUserAccountRepository
{
    IReadOnlyCollection<UserAccount> List();
    UserAccount? GetById(Guid id);
    UserAccount? GetByEmail(string email);
    void Add(UserAccount user);
    void Update(UserAccount user);
    void Delete(Guid id);
}
