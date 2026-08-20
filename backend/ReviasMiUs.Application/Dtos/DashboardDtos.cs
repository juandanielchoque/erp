namespace ReviasMiUs.Application.Dtos;

public sealed record DashboardDto(
    int LeadCount,
    int CustomerCount,
    int ProductCount,
    int LowStockProductCount,
    int ConfirmedOrderCount,
    decimal OpenSalesValue,
    IReadOnlyCollection<string> LowStockSkus);
