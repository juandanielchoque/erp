using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Orders;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemorySalesOrderRepository(InMemoryErpStore store) : ISalesOrderRepository
{
    public IReadOnlyCollection<SalesOrder> List() => store.Orders.ToArray();

    public SalesOrder? GetById(Guid id) => store.Orders.FirstOrDefault(order => order.Id == id);

    public void Add(SalesOrder order) => store.Orders.Add(order);

    public void Update(SalesOrder order)
    {
        var index = store.Orders.FindIndex(item => item.Id == order.Id);
        if (index >= 0)
        {
            store.Orders[index] = order;
        }
    }
    public void Delete(Guid id) => store.Orders.RemoveAll(item => item.Id == id);
}
