using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Settings;

public sealed class SystemSettings : Entity
{
    public string BusinessName { get; private set; } = "Sabor Peruano";
    public string TaxId { get; private set; } = "";
    public string Currency { get; private set; } = "PEN";
    public decimal TaxRate { get; private set; } = 18m;
    public string TimeZone { get; private set; } = "America/Lima";
    public string ReceiptSeries { get; private set; } = "B001";
    public string InvoiceSeries { get; private set; } = "F001";
    public bool RequireTableForDineIn { get; private set; } = true;
    public string? ReceiptLogoDataUrl { get; private set; }
    public int ReceiptPaperWidthMm { get; private set; } = 80;
    public string ReceiptAlignment { get; private set; } = "Center";
    public string ReceiptDensity { get; private set; } = "Normal";
    public string ReceiptHeaderText { get; private set; } = "Cocina peruana hecha con cariño";
    public string ReceiptFooterText { get; private set; } = "Gracias por tu visita. ¡Vuelve pronto!";
    public bool ReceiptShowBusinessName { get; private set; } = true;
    public bool ReceiptShowTaxId { get; private set; } = true;
    public bool ReceiptShowCashier { get; private set; } = true;
    public bool ReceiptShowCustomer { get; private set; } = true;
    public bool ReceiptShowPaymentMethod { get; private set; } = true;
    public bool ReceiptShowQrPlaceholder { get; private set; } = true;

    public void Update(string businessName, string taxId, string currency, decimal taxRate, string timeZone, string receiptSeries, string invoiceSeries, bool requireTableForDineIn)
    {
        if (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(timeZone))
            throw new DomainException("Business name, currency and time zone are required.");
        if (!string.IsNullOrWhiteSpace(taxId) && (taxId.Length != 11 || !taxId.All(char.IsDigit)))
            throw new DomainException("The business RUC must contain 11 digits.");
        if (taxRate is < 0 or > 100) throw new DomainException("Tax rate must be between 0 and 100.");
        if (string.IsNullOrWhiteSpace(receiptSeries) || string.IsNullOrWhiteSpace(invoiceSeries))
            throw new DomainException("Receipt and invoice series are required.");
        BusinessName = businessName.Trim();
        TaxId = taxId.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        TaxRate = taxRate;
        TimeZone = timeZone.Trim();
        ReceiptSeries = receiptSeries.Trim().ToUpperInvariant();
        InvoiceSeries = invoiceSeries.Trim().ToUpperInvariant();
        RequireTableForDineIn = requireTableForDineIn;
    }

    public void ConfigureReceipt(string? logoDataUrl, int paperWidthMm, string alignment, string density, string headerText, string footerText, bool showBusinessName, bool showTaxId, bool showCashier, bool showCustomer, bool showPaymentMethod, bool showQrPlaceholder)
    {
        if (paperWidthMm is not (58 or 80)) throw new DomainException("Receipt paper width must be 58 or 80 mm.");
        if (alignment is not ("Left" or "Center")) throw new DomainException("Invalid receipt alignment.");
        if (density is not ("Compact" or "Normal" or "Large")) throw new DomainException("Invalid receipt density.");
        if (headerText.Length > 160 || footerText.Length > 240) throw new DomainException("Receipt texts are too long.");
        var validLogoType = string.IsNullOrWhiteSpace(logoDataUrl) ||
            logoDataUrl.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase) ||
            logoDataUrl.StartsWith("data:image/jpeg", StringComparison.OrdinalIgnoreCase) ||
            logoDataUrl.StartsWith("data:image/webp", StringComparison.OrdinalIgnoreCase);
        if (!validLogoType || logoDataUrl?.Length > 500_000)
            throw new DomainException("The receipt logo must be a PNG, JPG or WEBP image smaller than 375 KB.");
        ReceiptLogoDataUrl = string.IsNullOrWhiteSpace(logoDataUrl) ? null : logoDataUrl;
        ReceiptPaperWidthMm = paperWidthMm;
        ReceiptAlignment = alignment;
        ReceiptDensity = density;
        ReceiptHeaderText = headerText.Trim();
        ReceiptFooterText = footerText.Trim();
        ReceiptShowBusinessName = showBusinessName;
        ReceiptShowTaxId = showTaxId;
        ReceiptShowCashier = showCashier;
        ReceiptShowCustomer = showCustomer;
        ReceiptShowPaymentMethod = showPaymentMethod;
        ReceiptShowQrPlaceholder = showQrPlaceholder;
    }
}
