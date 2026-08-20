using ReviasMiUs.Domain.Customers;

namespace ReviasMiUs.Application.Abstractions;

public interface ICustomerRepository
{
    IReadOnlyCollection<Customer> List();
    Customer? GetById(Guid id);
    void Add(Customer customer);
    void Update(Customer customer);
    void Delete(Guid id);
}
