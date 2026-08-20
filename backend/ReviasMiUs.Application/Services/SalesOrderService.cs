using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Orders;

namespace ReviasMiUs.Application.Services;

public sealed class SalesOrderService(
    ICustomerRepository customers,
    IProductRepository products,
    ISalesOrderRepository orders)
{
    public SalesOrderDto CreateQuotation(CreateSalesOrderRequest request)
    {
        var customer = customers.GetById(request.CustomerId)
            ?? throw new DomainException("Customer not found.");

        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new DomainException("A quotation requires at least one line.");
        }

        var normalizedLines = request.Lines
            .GroupBy(line => line.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToArray();

        var productSnapshots = normalizedLines
            .Select(line => new ProductSnapshot(
                products.GetById(line.ProductId)
                    ?? throw new DomainException("One of the products was not found."),
                line.Quantity))
            .ToArray();

        var validUntilUtc = request.ValidUntilUtc ?? DateTime.UtcNow.AddDays(30);
        if (!Enum.TryParse<OrderServiceType>(request.ServiceType, true, out var serviceType))
        {
            throw new DomainException("Invalid order service type.");
        }

        var order = new SalesOrder(
            customer.Id,
            BuildOrderNumber(),
            validUntilUtc,
            request.Notes,
            serviceType,
            request.TableNumber);

        foreach (var snapshot in productSnapshots)
        {
            order.AddLine(new OrderLine(snapshot.Product.Id, snapshot.Quantity, snapshot.Product.UnitPrice));
        }

        orders.Add(order);
        return Map(order, customer.Name, productSnapshots);
    }

    public SalesOrderDto GetById(Guid id)
    {
        var order = GetOrder(id);
        return MapWithCurrentNames(order);
    }

    public IReadOnlyCollection<SalesOrderDto> List(string? status = null, Guid? customerId = null)
    {
        var query = orders.List().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SalesOrderStatus>(status, true, out var parsedStatus))
            {
                throw new DomainException("Invalid sales order status.");
            }

            query = query.Where(order => order.Status == parsedStatus);
        }

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        var customerLookup = customers.List()
            .ToDictionary(customer => customer.Id, customer => customer.Name);
        var productLookup = products.List()
            .ToDictionary(product => product.Id, product => product.Name);

        return query
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => Map(
                order,
                customerLookup.GetValueOrDefault(order.CustomerId, "Unknown"),
                productLookup))
            .ToArray();
    }

    public SalesOrderDto Confirm(Guid id)
    {
        var order = GetOrder(id);
        if (order.Status != SalesOrderStatus.Draft)
        {
            throw new DomainException("Only draft quotations can be confirmed.");
        }

        var productSnapshots = LoadProducts(order);
        foreach (var snapshot in productSnapshots)
        {
            if (snapshot.Product.StockQuantity < snapshot.Quantity)
            {
                throw new DomainException($"Insufficient stock for {snapshot.Product.Sku}.");
            }
        }

        order.Confirm();
        foreach (var snapshot in productSnapshots)
        {
            snapshot.Product.AdjustStock(-snapshot.Quantity);
            products.Update(snapshot.Product);
        }

        orders.Update(order);
        return MapWithCurrentNames(order);
    }

    public SalesOrderDto UpdateQuotation(Guid id, CreateSalesOrderRequest request)
    {
        var order = GetOrder(id);
        if (order.Status != SalesOrderStatus.Draft) throw new DomainException("Only draft quotations can be edited.");
        var customer = customers.GetById(request.CustomerId) ?? throw new DomainException("Customer not found.");
        if (request.Lines is null || request.Lines.Count == 0) throw new DomainException("A quotation requires at least one line.");
        if (!Enum.TryParse<OrderServiceType>(request.ServiceType, true, out var serviceType))
            throw new DomainException("Invalid order service type.");

        var productSnapshots = request.Lines
            .GroupBy(line => line.ProductId)
            .Select(group => new ProductSnapshot(
                products.GetById(group.Key) ?? throw new DomainException("One of the products was not found."),
                group.Sum(item => item.Quantity)))
            .ToArray();
        var lines = productSnapshots.Select(item => new OrderLine(item.Product.Id, item.Quantity, item.Product.UnitPrice));
        order.UpdateDraft(request.CustomerId, request.ValidUntilUtc ?? DateTime.UtcNow.AddDays(30), request.Notes, serviceType, request.TableNumber, lines);
        orders.Update(order);
        return Map(order, customer.Name, productSnapshots);
    }

    public void DeleteDraft(Guid id)
    {
        var order = GetOrder(id);
        if (order.Status != SalesOrderStatus.Draft)
            throw new DomainException("Only draft quotations can be deleted; confirmed sales must be cancelled.");
        orders.Delete(id);
    }

    public SalesOrderDto Cancel(Guid id)
    {
        var order = GetOrder(id);
        var wasConfirmed = order.Status == SalesOrderStatus.Confirmed;

        order.Cancel();
        if (wasConfirmed)
        {
            foreach (var snapshot in LoadProducts(order))
            {
                snapshot.Product.AdjustStock(snapshot.Quantity);
                products.Update(snapshot.Product);
            }
        }

        orders.Update(order);
        return MapWithCurrentNames(order);
    }

    private SalesOrder GetOrder(Guid id) =>
        orders.GetById(id) ?? throw new DomainException("Sales document not found.");

    private ProductSnapshot[] LoadProducts(SalesOrder order) => order.Lines
        .Select(line => new ProductSnapshot(
            products.GetById(line.ProductId)
                ?? throw new DomainException("One of the products was not found."),
            line.Quantity))
        .ToArray();

    private SalesOrderDto MapWithCurrentNames(SalesOrder order)
    {
        var customerName = customers.GetById(order.CustomerId)?.Name ?? "Unknown";
        var productLookup = products.List().ToDictionary(product => product.Id, product => product.Name);
        return Map(order, customerName, productLookup);
    }

    private string BuildOrderNumber()
    {
        var sequence = orders.List().Count + 1;
        return $"S-{DateTime.UtcNow:yyyyMMdd}-{sequence:0000}";
    }

    private static SalesOrderDto Map(
        SalesOrder order,
        string customerName,
        IReadOnlyDictionary<Guid, string> productLookup)
    {
        var lines = order.Lines
            .Select(line => new OrderLineDto(
                line.ProductId,
                productLookup.GetValueOrDefault(line.ProductId, "Unknown"),
                line.Quantity,
                line.UnitPrice,
                line.LineTotal))
            .ToArray();

        return new SalesOrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            customerName,
            order.Status.ToString(),
            order.CreatedAtUtc,
            order.ValidUntilUtc,
            order.ConfirmedAtUtc,
            order.CancelledAtUtc,
            order.Notes,
            order.ServiceType.ToString(),
            order.TableNumber,
            order.TotalAmount,
            lines);
    }

    private static SalesOrderDto Map(
        SalesOrder order,
        string customerName,
        IReadOnlyCollection<ProductSnapshot> productSnapshots)
    {
        var productLookup = productSnapshots.ToDictionary(item => item.Product.Id, item => item.Product.Name);
        return Map(order, customerName, productLookup);
    }

    private sealed record ProductSnapshot(Product Product, int Quantity);
}
