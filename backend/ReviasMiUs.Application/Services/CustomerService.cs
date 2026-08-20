using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Customers;
using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Application.Services;

public sealed class CustomerService(ICustomerRepository customers, ISalesOrderRepository? orders = null)
{
    public CustomerDto Create(CreateCustomerRequest request)
    {
        EnsureUniqueEmail(request.Email);
        var customer = new Customer(request.Name, request.Email);
        customers.Add(customer);
        return Map(customer);
    }

    public IReadOnlyCollection<CustomerDto> List() =>
        customers.List()
            .OrderBy(customer => customer.Name)
            .Select(Map)
            .ToArray();

    public IReadOnlyCollection<CustomerDto> Search(string term)
    {
        var normalized = term.Trim();
        return customers.List()
            .Where(customer =>
                customer.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                customer.Email.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(customer => customer.Name)
            .Select(Map)
            .ToArray();
    }

    public Customer? FindById(Guid id) => customers.GetById(id);

    public CustomerDto Update(Guid id, UpdateCustomerRequest request)
    {
        var customer = customers.GetById(id) ?? throw new DomainException("Customer not found.");
        EnsureUniqueEmail(request.Email, id);
        customer.UpdateDetails(request.Name, request.Email);
        if (request.IsActive) customer.Activate(); else customer.Deactivate();
        customers.Update(customer);
        return Map(customer);
    }

    public void Delete(Guid id)
    {
        _ = customers.GetById(id) ?? throw new DomainException("Customer not found.");
        if (orders?.List().Any(order => order.CustomerId == id) == true)
            throw new DomainException("Customers with sales history cannot be deleted; deactivate them instead.");
        customers.Delete(id);
    }

    private void EnsureUniqueEmail(string email, Guid? excludedId = null)
    {
        if (customers.List().Any(item => item.Id != excludedId && string.Equals(item.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A customer with this email already exists.");
    }

    private static CustomerDto Map(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.IsActive, customer.CreatedAtUtc);
}
