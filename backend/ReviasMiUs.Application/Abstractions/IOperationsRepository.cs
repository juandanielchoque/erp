using ReviasMiUs.Domain.Operations;

namespace ReviasMiUs.Application.Abstractions;

public interface IOperationsRepository
{
    IReadOnlyCollection<RestaurantTable> ListTables();
    RestaurantTable? GetTable(string number);
    RestaurantTable? GetTableByQrToken(string token);
    void AddTable(RestaurantTable table);
    void UpdateTable(RestaurantTable table);
    void DeleteTable(Guid id);
    IReadOnlyCollection<CashShift> ListShifts();
    CashShift? GetShift(Guid id);
    void AddShift(CashShift shift);
    void UpdateShift(CashShift shift);
    IReadOnlyCollection<KitchenTicket> ListKitchenTickets();
    KitchenTicket? GetKitchenTicket(Guid id);
    void AddKitchenTicket(KitchenTicket ticket);
    void UpdateKitchenTicket(KitchenTicket ticket);
    IReadOnlyCollection<FiscalDocument> ListFiscalDocuments();
    FiscalDocument? GetFiscalDocument(Guid id);
    void AddFiscalDocument(FiscalDocument document);
    void UpdateFiscalDocument(FiscalDocument document);
}
