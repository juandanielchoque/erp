using ReviasMiUs.Domain.Customers;
using ReviasMiUs.Domain.Crm;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Users;
using ReviasMiUs.Domain.Operations;
using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Security;

namespace ReviasMiUs.Infrastructure.Persistence;

public static class SeedData
{
    public static void Initialize(InMemoryErpStore store, IPasswordHasher passwords)
    {
        if (store.Customers.Count > 0 || store.Products.Count > 0)
        {
            return;
        }

        var customers = new[]
        {
            new Customer("Cliente mostrador", "mostrador@revias.local"),
            new Customer("Empresa Los Andes", "pedidos@losandes.pe"),
            new Customer("Eventos Lima", "reservas@eventoslima.pe")
        };

        var products = new[]
        {
            new Product("Lomo saltado", "FON-001", 32.00m, 25, 6, "Fondos"),
            new Product("Aji de gallina", "FON-002", 25.00m, 20, 5, "Fondos"),
            new Product("Arroz con pollo", "FON-003", 22.00m, 24, 5, "Fondos"),
            new Product("Ceviche clasico", "MAR-001", 34.00m, 16, 4, "Marinos"),
            new Product("Leche de tigre", "MAR-002", 18.00m, 14, 4, "Marinos"),
            new Product("Causa limena", "ENT-001", 16.00m, 18, 4, "Entradas"),
            new Product("Tequenos", "ENT-002", 14.00m, 30, 8, "Entradas"),
            new Product("Chicha morada", "BEB-001", 8.00m, 35, 10, "Bebidas"),
            new Product("Maracuya", "BEB-002", 8.00m, 28, 8, "Bebidas"),
            new Product("Gaseosa personal", "BEB-003", 6.00m, 40, 12, "Bebidas"),
            new Product("Suspiro limeno", "POS-001", 12.00m, 15, 4, "Postres"),
            new Product("Torta de chocolate", "POS-002", 14.00m, 12, 3, "Postres")
        };

        var leads = new[]
        {
            new Lead("Carlos Rojas", "Andes Traders", "carlos@andestraders.com", "+51 999 111 222", "Website", 82),
            new Lead("Mariana Vega", "Northwind Peru", "mariana@northwind.pe", "+51 988 222 333", "Referral", 68),
            new Lead("Jorge Salas", "Mercado Local", "jorge@mercadolocal.pe", null, "Email", 91)
        };

        var roles = new[]
        {
            new RoleDefinition("Administrator", "Administrador", "Control total y permisos críticos del sistema.", Permissions.All, true),
            new RoleDefinition("Cashier", "Cajero", "Punto de venta, caja, mesas y comprobantes.", new[] { Permissions.DashboardView, Permissions.ProductsView, Permissions.CustomersView, Permissions.CustomersManage, Permissions.PosUse, Permissions.TablesView, Permissions.TablesRelease, Permissions.CashShiftsManage, Permissions.KitchenManage, Permissions.BillingView }, true),
            new RoleDefinition("Sales", "Ventas", "Clientes, CRM, cotizaciones y pedidos.", new[] { Permissions.DashboardView, Permissions.ProductsView, Permissions.CustomersView, Permissions.CustomersManage, Permissions.CrmManage, Permissions.SalesManage }, true),
            new RoleDefinition("Warehouse", "Almacén", "Consulta de inventario y operación de cocina.", new[] { Permissions.DashboardView, Permissions.ProductsView, Permissions.TablesView, Permissions.KitchenManage }, true)
        };

        var users = new[]
        {
            new UserAccount("Ana Mendoza", "admin@revias.local", UserRole.Administrator, passwords.Hash("Admin123!")),
            new UserAccount("Luis Paredes", "caja@revias.local", UserRole.Cashier, passwords.Hash("Caja123!")),
            new UserAccount("Rosa Torres", "almacen@revias.local", UserRole.Warehouse, passwords.Hash("Almacen123!"))
        };

        var tables = Enumerable.Range(1, 12)
            .Select(number => new RestaurantTable($"Mesa {number:00}", number <= 8 ? "Salon principal" : "Terraza", number % 3 == 0 ? 6 : 4))
            .ToArray();

        store.Customers.AddRange(customers);
        store.Leads.AddRange(leads);
        store.Products.AddRange(products);
        store.Roles.AddRange(roles);
        store.Users.AddRange(users);
        store.Tables.AddRange(tables);
    }
}
