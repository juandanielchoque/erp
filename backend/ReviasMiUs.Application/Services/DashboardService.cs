using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;

namespace ReviasMiUs.Application.Services;

public sealed class DashboardService(
    ILeadRepository leads,
    ICustomerRepository customers,
    IProductRepository products,
    ISalesOrderRepository orders)
{
    public DashboardDto GetOverview()
    {
        var leadCount = leads.List().Count;
        var customerCount = customers.List().Count(customer => customer.IsActive);
        var productCount = products.List().Count(product => product.IsActive);
        var lowStockProducts = products.List().Where(product => product.NeedsReorder()).ToArray();
        var confirmedOrders = orders.List().Where(order => order.Status == Domain.Orders.SalesOrderStatus.Confirmed).ToArray();
        var openSalesValue = confirmedOrders.Sum(order => order.TotalAmount);

        return new DashboardDto(
            leadCount,
            customerCount,
            productCount,
            lowStockProducts.Length,
            confirmedOrders.Length,
            openSalesValue,
            lowStockProducts.Select(product => product.Sku).OrderBy(sku => sku).ToArray());
    }
}
