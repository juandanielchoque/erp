using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Operations;

namespace ReviasMiUs.Tests;

public sealed class RestaurantOperationsTests
{
    [Fact]
    public void CashShift_CalculatesExpectedCashAndClosingDifference()
    {
        var shift = new CashShift(Guid.NewGuid(), "Caja principal", 100m);

        shift.RegisterPayment(Guid.NewGuid(), PaymentMethod.Cash, 59m);
        shift.RegisterPayment(Guid.NewGuid(), PaymentMethod.Yape, 25m);
        shift.AddCashMovement(CashMovementType.CashIn, 20m, "Cambio adicional");
        shift.AddCashMovement(CashMovementType.CashOut, 10m, "Compra urgente");
        shift.Close(170m);

        Assert.Equal(84m, shift.GrossSales);
        Assert.Equal(169m, shift.ExpectedCash);
        Assert.Equal(1m, shift.Difference);
        Assert.Equal(CashShiftStatus.Closed, shift.Status);
    }

    [Fact]
    public void CashShift_RegistersSplitPaymentWithoutDuplicatingSaleTotal()
    {
        var shift = new CashShift(Guid.NewGuid(), "Caja tactil", 100m);
        var orderId = Guid.NewGuid();

        shift.RegisterPayment(orderId, PaymentMethod.Cash, 42m);
        shift.RegisterPayment(orderId, PaymentMethod.Yape, 30m);

        Assert.Equal(72m, shift.GrossSales);
        Assert.Equal(142m, shift.ExpectedCash);
        Assert.Equal(2, shift.Movements.Count(movement => movement.OrderId == orderId));
    }

    [Fact]
    public void Invoice_RequiresElevenDigitRuc()
    {
        var exception = Assert.Throws<DomainException>(() => new FiscalDocument(
            Guid.NewGuid(),
            FiscalDocumentType.Invoice,
            "F001-00000001",
            "Cliente Empresa",
            "12345678",
            118m));

        Assert.Contains("11-digit RUC", exception.Message);
    }

    [Fact]
    public void FiscalDocument_SeparatesIncludedTax()
    {
        var document = new FiscalDocument(
            Guid.NewGuid(),
            FiscalDocumentType.Receipt,
            "B001-00000001",
            "Consumidor final",
            null,
            118m);

        Assert.Equal(100m, document.TaxableAmount);
        Assert.Equal(18m, document.TaxAmount);
        Assert.Equal(FiscalDocumentStatus.Issued, document.Status);
    }

    [Fact]
    public void KitchenTicket_AdvancesInOperationalOrder()
    {
        var ticket = new KitchenTicket(Guid.NewGuid(), "SO-0001", "Mesa 01");

        ticket.Advance();
        Assert.Equal(KitchenTicketStatus.Preparing, ticket.Status);
        Assert.NotNull(ticket.StartedAtUtc);

        ticket.Advance();
        Assert.Equal(KitchenTicketStatus.Ready, ticket.Status);
        Assert.NotNull(ticket.ReadyAtUtc);

        ticket.Advance();
        Assert.Equal(KitchenTicketStatus.Delivered, ticket.Status);
        Assert.Throws<DomainException>(ticket.Advance);
    }

    [Fact]
    public void RestaurantTable_RequiresCleaningBeforeRelease()
    {
        var table = new RestaurantTable("Mesa 01", "Salon", 4);

        table.Occupy(Guid.NewGuid());
        table.MarkCleaning();
        table.Release();

        Assert.Equal(RestaurantTableStatus.Free, table.Status);
        Assert.Null(table.CurrentOrderId);
        Assert.Null(table.OccupiedAtUtc);
    }

    [Fact]
    public void RestaurantTable_QrTokenCanBeRevoked()
    {
        var table = new RestaurantTable("Mesa 02", "Salon", 4);
        var originalToken = table.QrToken;

        table.RegenerateQrToken();

        Assert.Equal(32, table.QrToken.Length);
        Assert.NotEqual(originalToken, table.QrToken);
    }
}
