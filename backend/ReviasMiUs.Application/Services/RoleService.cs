using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Application.Security;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Application.Services;

public sealed class RoleService(IRoleRepository roles, IUserAccountRepository users)
{
    private static readonly PermissionDto[] Catalog =
    [
        new(Permissions.DashboardView, "Ver panel general", "Indicadores generales del negocio.", "General", false),
        new(Permissions.ProductsView, "Ver productos e inventario", "Consulta catálogo, precios y existencias.", "Inventario", false),
        new(Permissions.ProductsManage, "Administrar productos y precios", "Crea, edita y elimina productos o precios.", "Inventario", true),
        new(Permissions.CustomersView, "Ver clientes", "Consulta la cartera de clientes.", "Clientes", false),
        new(Permissions.CustomersManage, "Crear y editar clientes", "Registra y actualiza clientes.", "Clientes", false),
        new(Permissions.CustomersDelete, "Eliminar clientes", "Elimina clientes sin historial relacionado.", "Clientes", true),
        new(Permissions.CrmManage, "Gestionar CRM", "Administra oportunidades comerciales.", "Comercial", false),
        new(Permissions.SalesManage, "Gestionar ventas", "Crea cotizaciones, pedidos y confirmaciones.", "Comercial", false),
        new(Permissions.PosUse, "Usar punto de venta", "Registra pedidos, pagos y comprobantes.", "Operación", false),
        new(Permissions.TablesView, "Ver mesas", "Consulta el estado del salón.", "Operación", false),
        new(Permissions.TablesManage, "Configurar mesas", "Crea, edita y elimina mesas.", "Operación", true),
        new(Permissions.TablesRelease, "Liberar mesas", "Marca mesas limpias como disponibles.", "Operación", false),
        new(Permissions.CashShiftsManage, "Operar caja", "Abre, mueve y cierra su propio turno de caja.", "Operación", false),
        new(Permissions.KitchenManage, "Gestionar cocina", "Consulta y avanza comandas de cocina.", "Operación", false),
        new(Permissions.BillingView, "Ver comprobantes", "Consulta boletas y facturas internas.", "Facturación", false),
        new(Permissions.BillingCancel, "Anular comprobantes", "Anula comprobantes emitidos.", "Facturación", true),
        new(Permissions.UsersManage, "Administrar usuarios", "Crea, edita, bloquea y elimina cuentas.", "Seguridad", true),
        new(Permissions.RolesManage, "Administrar roles", "Crea roles y configura sus permisos.", "Seguridad", true),
        new(Permissions.SettingsManage, "Modificar ajustes", "Cambia datos empresariales, impuestos y series.", "Sistema", true)
    ];

    public IReadOnlyCollection<RoleDto> List() => roles.List().OrderBy(role => role.Name).Select(Map).ToArray();
    public IReadOnlyCollection<PermissionDto> ListPermissions() => Catalog;

    public RoleDto Create(CreateRoleRequest request)
    {
        if (roles.GetByCode(request.Code) is not null) throw new DomainException("A role with this code already exists.");
        var role = new RoleDefinition(request.Code, request.Name, request.Description, ValidatePermissions(request.Permissions));
        roles.Add(role);
        return Map(role);
    }

    public RoleDto Update(Guid id, UpdateRoleRequest request)
    {
        var role = Find(id);
        if (string.Equals(role.Code, UserRole.Administrator.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("The administrator role is protected and cannot be changed.");
        role.Update(request.Name, request.Description, ValidatePermissions(request.Permissions));
        roles.Update(role);
        return Map(role);
    }

    public void Delete(Guid id)
    {
        var role = Find(id);
        if (role.IsSystem) throw new DomainException("Default system roles cannot be deleted.");
        if (users.List().Any(user => string.Equals(user.Role, role.Code, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A role assigned to users cannot be deleted.");
        roles.Delete(id);
    }

    private static IReadOnlyCollection<string> ValidatePermissions(IReadOnlyCollection<string>? requested)
    {
        var values = (requested ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (values.Any(value => !Permissions.All.Contains(value, StringComparer.OrdinalIgnoreCase)))
            throw new DomainException("The role contains an unknown permission.");
        if (values.Any(Permissions.AdministratorOnly.Contains))
            throw new DomainException("Administrator-only permissions cannot be assigned to another role.");
        var expanded = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase) { Permissions.DashboardView };
        if (expanded.Contains(Permissions.CustomersManage)) expanded.Add(Permissions.CustomersView);
        if (expanded.Contains(Permissions.TablesRelease)) expanded.Add(Permissions.TablesView);
        if (expanded.Contains(Permissions.PosUse))
        {
            expanded.Add(Permissions.ProductsView);
            expanded.Add(Permissions.CustomersView);
            expanded.Add(Permissions.TablesView);
            expanded.Add(Permissions.CashShiftsManage);
            expanded.Add(Permissions.BillingView);
        }
        if (expanded.Contains(Permissions.SalesManage))
        {
            expanded.Add(Permissions.ProductsView);
            expanded.Add(Permissions.CustomersView);
        }
        return expanded.ToArray();
    }

    private RoleDefinition Find(Guid id) => roles.List().FirstOrDefault(role => role.Id == id) ?? throw new DomainException("Role not found.");
    private RoleDto Map(RoleDefinition role) => new(role.Id, role.Code, role.Name, role.Description, role.IsSystem,
        users.List().Count(user => string.Equals(user.Role, role.Code, StringComparison.OrdinalIgnoreCase)), role.Permissions);
}
