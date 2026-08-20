using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Settings;

namespace ReviasMiUs.Application.Services;

public sealed class SystemSettingsService(ISystemSettingsRepository repository)
{
    public SystemSettingsDto Get() => Map(repository.Get());
    public ReceiptTemplateDto GetReceiptTemplate() => MapReceipt(repository.Get());
    public SystemSettingsDto Update(UpdateSystemSettingsRequest request)
    {
        var settings = repository.Get();
        settings.Update(request.BusinessName, request.TaxId, request.Currency, request.TaxRate, request.TimeZone, request.ReceiptSeries, request.InvoiceSeries, request.RequireTableForDineIn);
        settings.ConfigureReceipt(request.ReceiptLogoDataUrl, request.ReceiptPaperWidthMm, request.ReceiptAlignment, request.ReceiptDensity, request.ReceiptHeaderText, request.ReceiptFooterText, request.ReceiptShowBusinessName, request.ReceiptShowTaxId, request.ReceiptShowCashier, request.ReceiptShowCustomer, request.ReceiptShowPaymentMethod, request.ReceiptShowQrPlaceholder);
        repository.Update(settings);
        return Map(settings);
    }
    private static SystemSettingsDto Map(SystemSettings item) => new(item.BusinessName, item.TaxId, item.Currency, item.TaxRate, item.TimeZone, item.ReceiptSeries, item.InvoiceSeries, item.RequireTableForDineIn, item.ReceiptLogoDataUrl, item.ReceiptPaperWidthMm, item.ReceiptAlignment, item.ReceiptDensity, item.ReceiptHeaderText, item.ReceiptFooterText, item.ReceiptShowBusinessName, item.ReceiptShowTaxId, item.ReceiptShowCashier, item.ReceiptShowCustomer, item.ReceiptShowPaymentMethod, item.ReceiptShowQrPlaceholder);
    public static ReceiptTemplateDto MapReceipt(SystemSettings item) => new(item.BusinessName, item.TaxId, item.Currency, item.TaxRate, item.ReceiptLogoDataUrl, item.ReceiptPaperWidthMm, item.ReceiptAlignment, item.ReceiptDensity, item.ReceiptHeaderText, item.ReceiptFooterText, item.ReceiptShowBusinessName, item.ReceiptShowTaxId, item.ReceiptShowCashier, item.ReceiptShowCustomer, item.ReceiptShowPaymentMethod, item.ReceiptShowQrPlaceholder);
}
