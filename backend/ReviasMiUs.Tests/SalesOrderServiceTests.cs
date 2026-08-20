using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Application.Services;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Customers;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Orders;
using ReviasMiUs.Infrastructure.Persistence;

namespace ReviasMiUs.Tests;

public sealed class SalesOrderServiceTests
{
    [Fact]
    public void DineInOrder_RequiresTableNumber()
    {
        Assert.Throws<DomainException>(() => new SalesOrder(
            Guid.NewGuid(),
            "S-TEST-001",
            DateTime.UtcNow.AddHours(1),
            serviceType: OrderServiceType.DineIn));
    }

    [Fact]
    public void CreateQuotation_LeavesStockUntouched()
    {
        var fixture = CreateFixture(stock: 10);

        var quotation = fixture.Service.CreateQuotation(CreateRequest(fixture, quantity: 3));

        Assert.Equal("Draft", quotation.Status);
        Assert.Equal(10, fixture.Product.StockQuantity);
        Assert.Equal(75m, quotation.TotalAmount);
    }

    [Fact]
    public void Confirm_AndCancel_AdjustStockInBothDirections()
    {
        var fixture = CreateFixture(stock: 10);
        var quotation = fixture.Service.CreateQuotation(CreateRequest(fixture, quantity: 3));

        var confirmed = fixture.Service.Confirm(quotation.Id);
        Assert.Equal("Confirmed", confirmed.Status);
        Assert.Equal(7, fixture.Product.StockQuantity);

        var cancelled = fixture.Service.Cancel(quotation.Id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(10, fixture.Product.StockQuantity);
    }

    [Fact]
    public void Confirm_RejectsOrderWhenStockIsInsufficient()
    {
        var fixture = CreateFixture(stock: 2);
        var quotation = fixture.Service.CreateQuotation(CreateRequest(fixture, quantity: 3));

        var exception = Assert.Throws<DomainException>(() => fixture.Service.Confirm(quotation.Id));

        Assert.Contains("Insufficient stock", exception.Message);
        Assert.Equal(2, fixture.Product.StockQuantity);
        Assert.Equal("Draft", fixture.Service.GetById(quotation.Id).Status);
    }

    private static CreateSalesOrderRequest CreateRequest(Fixture fixture, int quantity) =>
        new(fixture.Customer.Id, [new CreateOrderLineRequest(fixture.Product.Id, quantity)]);

    private static Fixture CreateFixture(int stock)
    {
        var store = new InMemoryErpStore();
        var customerRepository = new InMemoryCustomerRepository(store);
        var productRepository = new InMemoryProductRepository(store);
        var orderRepository = new InMemorySalesOrderRepository(store);
        var customer = new Customer("Test Customer", "customer@test.local");
        var product = new Product("Test Product", "TEST-001", 25m, stock, 1);

        customerRepository.Add(customer);
        productRepository.Add(product);

        return new Fixture(
            new SalesOrderService(customerRepository, productRepository, orderRepository),
            customer,
            product);
    }

    private sealed record Fixture(SalesOrderService Service, Customer Customer, Product Product);
}
