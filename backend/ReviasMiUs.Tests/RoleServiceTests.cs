using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Application.Security;
using ReviasMiUs.Application.Services;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Users;
using ReviasMiUs.Infrastructure.Persistence;

namespace ReviasMiUs.Tests;

public sealed class RoleServiceTests
{
    [Fact]
    public void Create_AddsOperationalDependenciesWithoutAdministrativePrivileges()
    {
        var fixture = CreateFixture();
        var role = fixture.Service.Create(new CreateRoleRequest("Waiter", "Mesero", "Atiende mesas", [Permissions.PosUse]));

        Assert.Contains(Permissions.PosUse, role.Permissions);
        Assert.Contains(Permissions.ProductsView, role.Permissions);
        Assert.Contains(Permissions.CashShiftsManage, role.Permissions);
        Assert.DoesNotContain(Permissions.ProductsManage, role.Permissions);
    }

    [Fact]
    public void Create_RejectsAdministratorOnlyPermission()
    {
        var fixture = CreateFixture();
        Assert.Throws<DomainException>(() => fixture.Service.Create(
            new CreateRoleRequest("Manager", "Gerente", "", [Permissions.ProductsManage])));
    }

    [Fact]
    public void Delete_RejectsRoleAssignedToUser()
    {
        var fixture = CreateFixture();
        var role = fixture.Service.Create(new CreateRoleRequest("Waiter", "Mesero", "", [Permissions.TablesView]));
        fixture.Store.Users.Add(new UserAccount("Mozo", "mozo@test.local", role.Code, "hash"));

        Assert.Throws<DomainException>(() => fixture.Service.Delete(role.Id));
    }

    private static (RoleService Service, InMemoryErpStore Store) CreateFixture()
    {
        var store = new InMemoryErpStore();
        return (new RoleService(new InMemoryRoleRepository(store), new InMemoryUserAccountRepository(store)), store);
    }
}
