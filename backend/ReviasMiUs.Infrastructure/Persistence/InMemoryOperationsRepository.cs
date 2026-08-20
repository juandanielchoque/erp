using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Operations;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryOperationsRepository(InMemoryErpStore store) : IOperationsRepository
{
    public IReadOnlyCollection<RestaurantTable> ListTables() => store.Tables.ToArray();
    public RestaurantTable? GetTable(string number) => store.Tables.FirstOrDefault(table => string.Equals(table.Number, number, StringComparison.OrdinalIgnoreCase));
    public RestaurantTable? GetTableByQrToken(string token) => store.Tables.FirstOrDefault(table => string.Equals(table.QrToken, token, StringComparison.Ordinal));
    public void AddTable(RestaurantTable table) => store.Tables.Add(table);
    public void UpdateTable(RestaurantTable table) => Replace(store.Tables, table);
    public void DeleteTable(Guid id) => store.Tables.RemoveAll(item => item.Id == id);
    public IReadOnlyCollection<CashShift> ListShifts() => store.CashShifts.ToArray();
    public CashShift? GetShift(Guid id) => store.CashShifts.FirstOrDefault(shift => shift.Id == id);
    public void AddShift(CashShift shift) => store.CashShifts.Add(shift);
    public void UpdateShift(CashShift shift) => Replace(store.CashShifts, shift);
    public IReadOnlyCollection<KitchenTicket> ListKitchenTickets() => store.KitchenTickets.ToArray();
    public KitchenTicket? GetKitchenTicket(Guid id) => store.KitchenTickets.FirstOrDefault(ticket => ticket.Id == id);
    public void AddKitchenTicket(KitchenTicket ticket) => store.KitchenTickets.Add(ticket);
    public void UpdateKitchenTicket(KitchenTicket ticket) => Replace(store.KitchenTickets, ticket);
    public IReadOnlyCollection<FiscalDocument> ListFiscalDocuments() => store.FiscalDocuments.ToArray();
    public FiscalDocument? GetFiscalDocument(Guid id) => store.FiscalDocuments.FirstOrDefault(document => document.Id == id);
    public void AddFiscalDocument(FiscalDocument document) => store.FiscalDocuments.Add(document);
    public void UpdateFiscalDocument(FiscalDocument document) => Replace(store.FiscalDocuments, document);

    private static void Replace<T>(List<T> items, T entity) where T : ReviasMiUs.Domain.Common.Entity
    {
        var index = items.FindIndex(item => item.Id == entity.Id);
        if (index >= 0) items[index] = entity;
    }
}
