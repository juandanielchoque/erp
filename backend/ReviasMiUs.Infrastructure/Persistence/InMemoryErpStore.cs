using ReviasMiUs.Domain.Customers;
using ReviasMiUs.Domain.Crm;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Orders;
using ReviasMiUs.Domain.Operations;
using ReviasMiUs.Domain.Users;
using ReviasMiUs.Domain.Settings;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryErpStore
{
    public List<Customer> Customers { get; } = [];
    public List<Lead> Leads { get; } = [];
    public List<Product> Products { get; } = [];
    public List<SalesOrder> Orders { get; } = [];
    public List<UserAccount> Users { get; } = [];
    public List<RoleDefinition> Roles { get; } = [];
    public List<RestaurantTable> Tables { get; } = [];
    public List<CashShift> CashShifts { get; } = [];
    public List<KitchenTicket> KitchenTickets { get; } = [];
    public List<FiscalDocument> FiscalDocuments { get; } = [];
    public SystemSettings Settings { get; } = new();
}
