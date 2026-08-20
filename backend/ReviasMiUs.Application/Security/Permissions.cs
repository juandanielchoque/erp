namespace ReviasMiUs.Application.Security;

public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string ProductsView = "products.view";
    public const string ProductsManage = "products.manage";
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";
    public const string CustomersDelete = "customers.delete";
    public const string CrmManage = "crm.manage";
    public const string SalesManage = "sales.manage";
    public const string PosUse = "pos.use";
    public const string TablesView = "tables.view";
    public const string TablesManage = "tables.manage";
    public const string TablesRelease = "tables.release";
    public const string CashShiftsManage = "cash-shifts.manage";
    public const string KitchenManage = "kitchen.manage";
    public const string BillingView = "billing.view";
    public const string BillingCancel = "billing.cancel";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyCollection<string> All = typeof(Permissions).GetFields()
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    public static readonly IReadOnlySet<string> AdministratorOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ProductsManage, CustomersDelete, TablesManage, BillingCancel, UsersManage, RolesManage, SettingsManage
    };
}
