using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Application.Services;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Infrastructure.Persistence;
using ReviasMiUs.Infrastructure.Security;

namespace ReviasMiUs.Tests;

public sealed class UserAccountServiceTests
{
    [Fact]
    public void Create_RejectsDuplicatedEmail()
    {
        var service = CreateService();
        var request = new CreateUserAccountRequest("Caja Uno", "caja@test.local", "Cashier", "Password123!");
        service.Create(request);

        Assert.Throws<DomainException>(() => service.Create(request));
    }

    [Fact]
    public void ChangeRoleAndStatus_UpdatesOperationalPermissions()
    {
        var service = CreateService();
        var user = service.Create(new CreateUserAccountRequest("Operador", "operador@test.local", "Cashier", "Password123!"));

        var withNewRole = service.ChangeRole(user.Id, new UpdateUserRoleRequest("Warehouse"));
        var inactive = service.ChangeStatus(user.Id, new UpdateUserStatusRequest(false));

        Assert.Equal("Warehouse", withNewRole.Role);
        Assert.False(inactive.IsActive);
    }

    private static UserAccountService CreateService()
    {
        var store = new InMemoryErpStore();
        store.Roles.Add(new("Cashier", "Cajero", "", []));
        store.Roles.Add(new("Warehouse", "Almacen", "", []));
        return new UserAccountService(new InMemoryUserAccountRepository(store), new InMemoryRoleRepository(store), new Pbkdf2PasswordHasher());
    }
}
