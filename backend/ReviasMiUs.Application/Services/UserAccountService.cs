using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Application.Services;

public sealed class UserAccountService(IUserAccountRepository users, IRoleRepository roles, IPasswordHasher passwords, IOperationsRepository? operations = null)
{
    public IReadOnlyCollection<UserAccountDto> List() => users.List()
        .OrderBy(user => user.Name)
        .Select(Map)
        .ToArray();

    public UserAccountDto Create(CreateUserAccountRequest request)
    {
        if (users.GetByEmail(request.Email) is not null)
        {
            throw new DomainException("A user with this email already exists.");
        }

        var user = new UserAccount(request.Name, request.Email, ValidateRole(request.Role), passwords.Hash(request.Password));
        users.Add(user);
        return Map(user);
    }

    public UserAccountDto ChangeRole(Guid id, UpdateUserRoleRequest request)
    {
        var user = Find(id);
        user.ChangeRole(ValidateRole(request.Role));
        users.Update(user);
        return Map(user);
    }

    public UserAccountDto ChangeStatus(Guid id, UpdateUserStatusRequest request)
    {
        var user = Find(id);
        if (!request.IsActive && operations?.ListShifts().Any(shift => shift.UserId == id && shift.Status == Domain.Operations.CashShiftStatus.Open) == true)
            throw new DomainException("A user with an open cash shift cannot be deactivated.");
        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        users.Update(user);
        return Map(user);
    }

    public UserAccountDto Update(Guid id, UpdateUserAccountRequest request)
    {
        var user = Find(id);
        var duplicate = users.GetByEmail(request.Email);
        if (duplicate is not null && duplicate.Id != id) throw new DomainException("A user with this email already exists.");
        if (!request.IsActive && operations?.ListShifts().Any(shift => shift.UserId == id && shift.Status == Domain.Operations.CashShiftStatus.Open) == true)
            throw new DomainException("A user with an open cash shift cannot be deactivated.");
        user.UpdateProfile(request.Name, request.Email, ValidateRole(request.Role));
        if (!string.IsNullOrWhiteSpace(request.NewPassword)) user.ChangePassword(passwords.Hash(request.NewPassword));
        if (request.IsActive) user.Activate(); else user.Deactivate();
        users.Update(user);
        return Map(user);
    }

    public void Delete(Guid id)
    {
        var user = Find(id);
        if (operations?.ListShifts().Any(shift => shift.UserId == id) == true)
            throw new DomainException("Users with cash shift history cannot be deleted; deactivate them instead.");
        if (string.Equals(user.Role, UserRole.Administrator.ToString(), StringComparison.OrdinalIgnoreCase) && users.List().Count(item => string.Equals(item.Role, UserRole.Administrator.ToString(), StringComparison.OrdinalIgnoreCase) && item.IsActive) <= 1)
            throw new DomainException("The last active administrator cannot be deleted.");
        users.Delete(id);
    }

    private UserAccount Find(Guid id) =>
        users.GetById(id) ?? throw new DomainException("User not found.");

    private string ValidateRole(string role) =>
        roles.GetByCode(role)?.Code ?? throw new DomainException("Invalid user role.");

    private static UserAccountDto Map(UserAccount user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.IsActive, user.CreatedAtUtc);
}
