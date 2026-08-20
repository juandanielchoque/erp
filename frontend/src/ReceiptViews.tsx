import { useEffect, useState } from "react";
import QRCode from "qrcode";
import type { PrintableReceipt, ReceiptTemplate, SalesOrder, FiscalDocument } from "./api";

export const previewOrder: SalesOrder = {
  id: "preview", orderNumber: "S-20260820-0042", customerId: "preview", customerName: "Cliente mostrador", status: "Confirmed", totalAmount: 72,
  createdAtUtc: new Date().toISOString(), validUntilUtc: new Date().toISOString(), serviceType: "DineIn", tableNumber: "Mesa 04",
  lines: [
    { productId: "1", productName: "Lomo saltado", quantity: 2, unitPrice: 32, lineTotal: 64 },
    { productId: "2", productName: "Chicha morada", quantity: 1, unitPrice: 8, lineTotal: 8 }
  ]
};

export const previewDocument: FiscalDocument = {
  id: "preview", orderId: "preview", type: "Receipt", number: "B001-00000042", customerName: "Cliente mostrador",
  taxableAmount: 61.02, taxAmount: 10.98, totalAmount: 72, status: "Issued", issuedAtUtc: new Date().toISOString()
};

export function ThermalReceipt({ receipt, preview = false }: { receipt: PrintableReceipt; preview?: boolean }) {
  const { template, document, order } = receipt;
  const money = new Intl.NumberFormat("es-PE", { style: "currency", currency: template.currency });
  const [qrImage, setQrImage] = useState("");
  useEffect(() => {
    if (!template.showQrPlaceholder) { setQrImage(""); return; }
    const payload = JSON.stringify({ ruc: template.taxId, document: document.number, issuedAt: document.issuedAtUtc, total: document.totalAmount, currency: template.currency });
    void QRCode.toDataURL(payload, { width: 180, margin: 1, color: { dark: "#000000", light: "#ffffff" } }).then(setQrImage);
  }, [document.issuedAtUtc, document.number, document.totalAmount, template.currency, template.showQrPlaceholder, template.taxId]);
  const align = template.alignment.toLowerCase();
  return <article className={`thermal-receipt paper-${template.paperWidthMm} density-${template.density.toLowerCase()} align-${align} ${preview ? "is-preview" : ""}`}>
    <header className="receipt-brand">
      {template.logoDataUrl && <img src={template.logoDataUrl} alt="Logo del negocio" />}
      {template.showBusinessName && <h2>{template.businessName}</h2>}
      {template.showTaxId && template.taxId && <p>RUC {template.taxId}</p>}
      {template.headerText && <em>{template.headerText}</em>}
    </header>
    <div className="receipt-rule">********************************</div>
    <section className="receipt-meta">
      <strong>{document.type === "Invoice" ? "FACTURA" : "BOLETA"} {document.number}</strong>
      <span>Fecha <b>{new Date(document.issuedAtUtc).toLocaleString("es-PE")}</b></span>
      <span>Pedido <b>{order.orderNumber}</b></span>
      {order.tableNumber && <span>Mesa <b>{order.tableNumber}</b></span>}
      {template.showCashier && <span>Cajero <b>{receipt.cashierName}</b></span>}
      {template.showCustomer && <span>Cliente <b>{document.customerName}</b></span>}
      {template.showPaymentMethod && receipt.payments.map(payment => <span key={payment.method}>Pago · {paymentLabel(payment.method)} <b>{money.format(payment.amount)}</b></span>)}
    </section>
    <div className="receipt-rule">--------------------------------</div>
    <section className="receipt-lines">
      {order.lines.map(line => <div key={line.productId}><span><b>{line.quantity}x</b> {line.productName}<small>{money.format(line.unitPrice)} c/u</small></span><strong>{money.format(line.lineTotal)}</strong></div>)}
    </section>
    <div className="receipt-rule">--------------------------------</div>
    <section className="receipt-totals"><span>Subtotal <b>{money.format(document.taxableAmount)}</b></span><span>IGV <b>{money.format(document.taxAmount)}</b></span><strong>TOTAL <b>{money.format(document.totalAmount)}</b></strong></section>
    {template.showQrPlaceholder && qrImage && <div className="receipt-qr"><img src={qrImage} alt="QR con datos del comprobante" /><small>Datos verificables del comprobante</small></div>}
    <footer>{template.footerText && <p>{template.footerText}</p>}<small>ReviasMiUs · Sistema de punto de venta</small></footer>
  </article>;
}

export function ReceiptModal({ receipt, onClose }: { receipt: PrintableReceipt; onClose: () => void }) {
  return <div className="modal-backdrop receipt-backdrop"><section className="receipt-modal" role="dialog" aria-modal="true"><header><div><span>COMPROBANTE LISTO</span><h3>Vista de impresión</h3></div><button onClick={onClose}>×</button></header><div className="receipt-stage"><ThermalReceipt receipt={receipt} /></div><footer><button onClick={onClose}>Cerrar</button><button className="primary-button" onClick={() => window.print()}>Imprimir ticket</button></footer></section></div>;
}

export function receiptFromPreview(template: ReceiptTemplate): PrintableReceipt {
  return { template, document: previewDocument, order: previewOrder, cashierName: "Luis Paredes", payments: [{ method: "Cash", amount: 42 }, { method: "Yape", amount: 30 }] };
}

function paymentLabel(method: string) {
  return ({ Cash: "Efectivo", Card: "Tarjeta", Yape: "Yape", Plin: "Plin", BankTransfer: "Transferencia", Pending: "Pendiente" } as Record<string, string>)[method] ?? method;
}
