using ReviasMiUs.Domain.Orders;

namespace ReviasMiUs.Application.Abstractions;

public interface ISalesOrderRepository
{
    IReadOnlyCollection<SalesOrder> List();
    SalesOrder? GetById(Guid id);
    void Add(SalesOrder order);
    void Update(SalesOrder order);
    void Delete(Guid id);
}
