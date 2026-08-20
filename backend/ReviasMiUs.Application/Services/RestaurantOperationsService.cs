using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Operations;

namespace ReviasMiUs.Application.Services;

public sealed class RestaurantOperationsService(
    IOperationsRepository operations,
    IUserAccountRepository users,
    ICustomerRepository customers,
    IProductRepository products,
    SalesOrderService sales,
    ISystemSettingsRepository settings)
{
    public IReadOnlyCollection<RestaurantTableDto> ListTables() => operations.ListTables()
        .OrderBy(table => table.Area)
        .ThenBy(table => table.Number)
        .Select(Map)
        .ToArray();

    public RestaurantTableDto CreateTable(CreateRestaurantTableRequest request)
    {
        if (operations.GetTable(request.Number) is not null) throw new DomainException("A table with this number already exists.");
        var table = new RestaurantTable(request.Number, request.Area, request.Seats);
        operations.AddTable(table);
        return Map(table);
    }

    public RestaurantTableDto UpdateTable(string currentNumber, UpdateRestaurantTableRequest request)
    {
        var table = operations.GetTable(currentNumber) ?? throw new DomainException("Table not found.");
        var duplicate = operations.GetTable(request.Number);
        if (duplicate is not null && duplicate.Id != table.Id) throw new DomainException("A table with this number already exists.");
        table.UpdateDetails(request.Number, request.Area, request.Seats);
        operations.UpdateTable(table);
        return Map(table);
    }

    public void DeleteTable(string number)
    {
        var table = operations.GetTable(number) ?? throw new DomainException("Table not found.");
        if (table.Status != RestaurantTableStatus.Free) throw new DomainException("Only free tables can be deleted.");
        operations.DeleteTable(table.Id);
    }

    public IReadOnlyCollection<CashShiftDto> ListShifts() => operations.ListShifts()
        .OrderByDescending(shift => shift.OpenedAtUtc)
        .Select(Map)
        .ToArray();

    public CashShiftDto OpenShift(OpenCashShiftRequest request)
    {
        var user = users.GetById(request.UserId) ?? throw new DomainException("User not found.");
        if (!user.IsActive) throw new DomainException("The user is inactive.");
        if (operations.ListShifts().Any(shift => shift.UserId == request.UserId && shift.Status == CashShiftStatus.Open))
        {
            throw new DomainException("The user already has an open cash shift.");
        }

        var shift = new CashShift(request.UserId, request.Terminal, request.OpeningCash);
        operations.AddShift(shift);
        return Map(shift);
    }

    public CashShiftDto CloseShift(Guid id, CloseCashShiftRequest request)
    {
        var shift = FindShift(id);
        shift.Close(request.CountedCash);
        operations.UpdateShift(shift);
        return Map(shift);
    }

    public CashShiftDto AddCashMovement(Guid id, CashMovementRequest request)
    {
        var shift = FindShift(id);
        if (!Enum.TryParse<CashMovementType>(request.Type, true, out var type))
            throw new DomainException("Invalid cash movement type.");
        shift.AddCashMovement(type, request.Amount, request.Reason);
        operations.UpdateShift(shift);
        return Map(shift);
    }

    public IReadOnlyCollection<KitchenTicketDto> ListKitchenTickets() => operations.ListKitchenTickets()
        .OrderBy(ticket => ticket.Status)
        .ThenBy(ticket => ticket.CreatedAtUtc)
        .Select(Map)
        .ToArray();

    public KitchenTicketDto AdvanceKitchenTicket(Guid id)
    {
        var ticket = operations.GetKitchenTicket(id) ?? throw new DomainException("Kitchen ticket not found.");
        ticket.Advance();
        operations.UpdateKitchenTicket(ticket);

        if (ticket.Status == KitchenTicketStatus.Delivered && !string.IsNullOrWhiteSpace(ticket.TableNumber))
        {
            var table = operations.GetTable(ticket.TableNumber);
            var hasActiveTickets = operations.ListKitchenTickets().Any(item =>
                item.Id != ticket.Id &&
                string.Equals(item.TableNumber, ticket.TableNumber, StringComparison.OrdinalIgnoreCase) &&
                item.Status is KitchenTicketStatus.Pending or KitchenTicketStatus.Preparing or KitchenTicketStatus.Ready);
            if (table?.Status == RestaurantTableStatus.Occupied && !hasActiveTickets)
            {
                table.MarkCleaning();
                operations.UpdateTable(table);
            }
        }
        return Map(ticket);
    }

    public KitchenTicketDto CancelKitchenTicket(Guid id)
    {
        var ticket = operations.GetKitchenTicket(id) ?? throw new DomainException("Kitchen ticket not found.");
        ticket.Cancel();
        operations.UpdateKitchenTicket(ticket);
        if (!string.IsNullOrWhiteSpace(ticket.TableNumber))
        {
            var table = operations.GetTable(ticket.TableNumber);
            var hasActiveTickets = operations.ListKitchenTickets().Any(item =>
                item.Id != ticket.Id &&
                string.Equals(item.TableNumber, ticket.TableNumber, StringComparison.OrdinalIgnoreCase) &&
                item.Status is KitchenTicketStatus.Pending or KitchenTicketStatus.Preparing or KitchenTicketStatus.Ready);
            if (table?.Status == RestaurantTableStatus.Occupied && !hasActiveTickets)
            {
                table.Release();
                operations.UpdateTable(table);
            }
        }
        return Map(ticket);
    }

    public RestaurantTableDto ReleaseTable(string number)
    {
        var table = operations.GetTable(number) ?? throw new DomainException("Table not found.");
        table.Release();
        operations.UpdateTable(table);
        return Map(table);
    }

    public RestaurantTableDto RegenerateTableQr(string number)
    {
        var table = operations.GetTable(number) ?? throw new DomainException("Table not found.");
        table.RegenerateQrToken();
        operations.UpdateTable(table);
        return Map(table);
    }

    public PublicTableMenuDto GetPublicMenu(string token)
    {
        var table = FindTableByQrToken(token);
        var configuration = settings.Get();
        var menu = products.List()
            .Where(product => product.IsActive && product.StockQuantity > 0)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Name)
            .Select(product => new PublicMenuProductDto(product.Id, product.Name, product.Category, product.UnitPrice, product.StockQuantity))
            .ToArray();

        return new PublicTableMenuDto(configuration.BusinessName, table.Number, table.Area, table.Seats, configuration.Currency, menu);
    }

    public PublicTableOrderDto CreatePublicTableOrder(string token, CreatePublicTableOrderRequest request)
    {
        var table = FindTableByQrToken(token);
        if (table.Status == RestaurantTableStatus.Cleaning)
            throw new DomainException("La mesa se encuentra en limpieza. Solicite ayuda al personal.");
        if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Count > 30)
            throw new DomainException("El pedido debe contener entre 1 y 30 productos.");
        if (request.Lines.Any(line => line.Quantity is < 1 or > 20))
            throw new DomainException("Cada producto permite entre 1 y 20 unidades.");
        if (request.GuestName?.Length > 80 || request.Notes?.Length > 500)
            throw new DomainException("El nombre o las observaciones son demasiado extensos.");

        var customer = customers.List().FirstOrDefault(item => item.IsActive)
            ?? throw new DomainException("No existe un cliente de mostrador activo.");
        var guest = string.IsNullOrWhiteSpace(request.GuestName) ? "Comensal" : request.GuestName.Trim();
        var notes = $"Pedido QR de {guest}. {request.Notes}".Trim();
        var order = sales.CreateQuotation(new CreateSalesOrderRequest(
            customer.Id,
            request.Lines,
            DateTime.UtcNow.AddHours(8),
            notes,
            "DineIn",
            table.Number));
        var confirmed = sales.Confirm(order.Id);

        if (table.Status == RestaurantTableStatus.Free)
        {
            table.Occupy(order.Id);
            operations.UpdateTable(table);
        }

        var ticket = new KitchenTicket(order.Id, order.OrderNumber, table.Number);
        operations.AddKitchenTicket(ticket);
        return new PublicTableOrderDto(order.Id, order.OrderNumber, table.Number, confirmed.TotalAmount, "Received", DateTime.UtcNow);
    }

    public IReadOnlyCollection<FiscalDocumentDto> ListFiscalDocuments() => operations.ListFiscalDocuments()
        .OrderByDescending(document => document.IssuedAtUtc)
        .Select(Map)
        .ToArray();

    public FiscalDocumentDto CancelFiscalDocument(Guid id)
    {
        var document = operations.GetFiscalDocument(id) ?? throw new DomainException("Fiscal document not found.");
        document.Cancel();
        operations.UpdateFiscalDocument(document);
        return Map(document);
    }

    public PrintableReceiptDto GetPrintableReceipt(Guid documentId)
    {
        var document = operations.GetFiscalDocument(documentId) ?? throw new DomainException("Fiscal document not found.");
        var order = sales.GetById(document.OrderId);
        var payments = operations.ListShifts()
            .SelectMany(shift => shift.Movements
                .Where(movement => movement.OrderId == document.OrderId && movement.Type == CashMovementType.Payment)
                .Select(movement => new { Shift = shift, Movement = movement }))
            .OrderBy(item => item.Movement.CreatedAtUtc)
            .ToArray();
        var cashierName = payments.Length == 0 ? "Venta digital" : users.GetById(payments[0].Shift.UserId)?.Name ?? "Usuario";
        var receiptPayments = payments.Length == 0
            ? [new ReceiptPaymentDto("Pending", document.TotalAmount)]
            : payments.Select(item => new ReceiptPaymentDto(item.Movement.Method.ToString(), item.Movement.Amount)).ToArray();
        return new PrintableReceiptDto(SystemSettingsService.MapReceipt(settings.Get()), Map(document), order, cashierName, receiptPayments);
    }

    public CompletePosSaleDto CompleteSale(CompletePosSaleRequest request)
    {
        var user = users.GetById(request.UserId) ?? throw new DomainException("User not found.");
        var customer = customers.GetById(request.CustomerId) ?? throw new DomainException("Customer not found.");
        var shift = operations.ListShifts().SingleOrDefault(item => item.UserId == user.Id && item.Status == CashShiftStatus.Open)
            ?? throw new DomainException("The user must open a cash shift before selling.");
        var payments = ParsePayments(request);
        if (!Enum.TryParse<FiscalDocumentType>(request.DocumentType, true, out var documentType))
            throw new DomainException("Invalid fiscal document type.");
        if (documentType == FiscalDocumentType.Invoice && (request.CustomerTaxId?.Length != 11))
            throw new DomainException("An 11-digit RUC is required for an invoice.");

        RestaurantTable? table = null;
        if (string.Equals(request.ServiceType, "DineIn", StringComparison.OrdinalIgnoreCase))
        {
            table = operations.GetTable(request.TableNumber ?? string.Empty) ?? throw new DomainException("Table not found.");
            if (table.Status != RestaurantTableStatus.Free) throw new DomainException("The table is not available.");
        }

        var order = sales.CreateQuotation(new CreateSalesOrderRequest(
            request.CustomerId,
            request.Lines,
            DateTime.UtcNow.AddHours(8),
            request.Notes,
            request.ServiceType,
            request.TableNumber));
        if (request.Payments is not { Count: > 0 })
            payments = [new ParsedPayment(payments.Single().Method, order.TotalAmount)];
        if (Math.Abs(payments.Sum(payment => payment.Amount) - order.TotalAmount) > 0.005m)
        {
            sales.DeleteDraft(order.Id);
            throw new DomainException("La suma de los medios de pago debe coincidir con el total de la venta.");
        }
        var confirmedOrder = sales.Confirm(order.Id);

        if (table is not null)
        {
            table.Occupy(order.Id);
            operations.UpdateTable(table);
        }

        foreach (var payment in payments)
            shift.RegisterPayment(order.Id, payment.Method, payment.Amount);
        operations.UpdateShift(shift);

        var ticket = new KitchenTicket(order.Id, order.OrderNumber, request.TableNumber);
        operations.AddKitchenTicket(ticket);

        var sequence = operations.ListFiscalDocuments().Count(document => document.Type == documentType) + 1;
        var configuration = settings.Get();
        var prefix = documentType == FiscalDocumentType.Invoice ? configuration.InvoiceSeries : configuration.ReceiptSeries;
        var document = new FiscalDocument(order.Id, documentType, $"{prefix}-{sequence:00000000}", customer.Name, request.CustomerTaxId, confirmedOrder.TotalAmount, configuration.TaxRate);
        operations.AddFiscalDocument(document);

        return new CompletePosSaleDto(confirmedOrder, Map(shift), Map(ticket), Map(document));
    }

    private CashShift FindShift(Guid id) => operations.GetShift(id) ?? throw new DomainException("Cash shift not found.");
    private static IReadOnlyCollection<ParsedPayment> ParsePayments(CompletePosSaleRequest request)
    {
        if (request.Payments is { Count: > 5 })
            throw new DomainException("Una venta permite como máximo cinco partes de pago.");
        if (request.Payments is { Count: > 0 } && request.Payments.Any(payment => payment.Amount <= 0 || decimal.Round(payment.Amount, 2) != payment.Amount))
            throw new DomainException("Cada medio de pago debe tener un monto positivo con máximo dos decimales.");
        var requested = request.Payments is { Count: > 0 }
            ? request.Payments
            : [new PaymentPartRequest(request.PaymentMethod, 0m)];
        var parsed = requested
            .GroupBy(payment => payment.Method, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (!Enum.TryParse<PaymentMethod>(group.Key, true, out var method))
                    throw new DomainException("Invalid payment method.");
                return new ParsedPayment(method, group.Sum(payment => payment.Amount));
            })
            .ToArray();
        return parsed;
    }

    private sealed record ParsedPayment(PaymentMethod Method, decimal Amount);
    private CashShiftDto Map(CashShift shift) => new(shift.Id, shift.UserId, users.GetById(shift.UserId)?.Name ?? "Unknown", shift.Terminal, shift.Status.ToString(), shift.OpeningCash, shift.ExpectedCash, shift.CountedCash, shift.Difference, shift.GrossSales, shift.OpenedAtUtc, shift.ClosedAtUtc);
    private RestaurantTable FindTableByQrToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 32)
            throw new DomainException("El codigo QR de la mesa no es valido.");
        return operations.GetTableByQrToken(token) ?? throw new DomainException("El codigo QR de la mesa no existe o fue renovado.");
    }

    private static RestaurantTableDto Map(RestaurantTable table) => new(table.Number, table.Area, table.Seats, table.Status.ToString(), table.CurrentOrderId, table.OccupiedAtUtc, table.QrToken);
    private KitchenTicketDto Map(KitchenTicket ticket)
    {
        var lines = sales.GetById(ticket.OrderId).Lines
            .Select(line => new KitchenTicketLineDto(line.ProductName, line.Quantity))
            .ToArray();
        return new KitchenTicketDto(ticket.Id, ticket.OrderId, ticket.OrderNumber, ticket.TableNumber, ticket.Status.ToString(), ticket.CreatedAtUtc, ticket.StartedAtUtc, ticket.ReadyAtUtc, lines);
    }
    private static FiscalDocumentDto Map(FiscalDocument document) => new(document.Id, document.OrderId, document.Type.ToString(), document.Number, document.CustomerName, document.CustomerTaxId, document.TaxableAmount, document.TaxAmount, document.TotalAmount, document.Status.ToString(), document.IssuedAtUtc);
}
