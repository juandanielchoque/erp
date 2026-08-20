using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Customers;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryCustomerRepository(InMemoryErpStore store) : ICustomerRepository
{
    public IReadOnlyCollection<Customer> List() => store.Customers.ToArray();

    public Customer? GetById(Guid id) => store.Customers.FirstOrDefault(customer => customer.Id == id);

    public void Add(Customer customer) => store.Customers.Add(customer);
    public void Update(Customer customer)
    {
        var index = store.Customers.FindIndex(item => item.Id == customer.Id);
        if (index >= 0) store.Customers[index] = customer;
    }
    public void Delete(Guid id) => store.Customers.RemoveAll(item => item.Id == id);
}
