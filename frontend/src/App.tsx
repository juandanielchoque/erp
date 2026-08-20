import { useEffect, useState } from "react";
import {
  advanceKitchenTicket,
  cancelKitchenTicket,
  closeCashShift,
  createTable,
  createPosSale,
  deleteTable,
  getPrintableReceipt,
  loadErpData,
  logout,
  openCashShift,
  PERMISSIONS,
  releaseTable,
  restoreSession,
  updateTable,
  type AuthUser,
  type CashShift,
  type Customer,
  type Dashboard,
  type FiscalDocument,
  type KitchenTicket,
  type Lead,
  type PaymentMethodName,
  type Product,
  type PrintableReceipt,
  type RestaurantTable,
  type SalesOrder,
  type UserAccount
} from "./api";
import { BillingManagement, CrmManagement, CustomersManagement, InventoryManagement, SalesManagement } from "./ManagementViews";
import { LoginView, SecureUsersView, SettingsView } from "./SecurityViews";
import { GuestOrderingView, TableQrModal } from "./QrOrderingViews";
import { ReceiptModal } from "./ReceiptViews";

type ModuleId = "overview" | "pos" | "tables" | "kitchen" | "crm" | "sales" | "inventory" | "customers" | "users" | "billing" | "settings";

interface ErpData {
  dashboard: Dashboard;
  leads: Lead[];
  products: Product[];
  orders: SalesOrder[];
  customers: Customer[];
  users: UserAccount[];
  tables: RestaurantTable[];
  shifts: CashShift[];
  kitchenTickets: KitchenTicket[];
  fiscalDocuments: FiscalDocument[];
}

const modules: Array<{ id: ModuleId; label: string; short: string; permissions: string[] }> = [
  { id: "overview", label: "Vista general", short: "VG", permissions: [PERMISSIONS.dashboard] },
  { id: "pos", label: "Punto de venta", short: "PV", permissions: [PERMISSIONS.pos] },
  { id: "tables", label: "Mesas y caja", short: "MC", permissions: [PERMISSIONS.tablesView, PERMISSIONS.cashShifts] },
  { id: "kitchen", label: "Cocina", short: "CO", permissions: [PERMISSIONS.kitchen] },
  { id: "crm", label: "CRM", short: "CR", permissions: [PERMISSIONS.crm] },
  { id: "sales", label: "Ventas", short: "VE", permissions: [PERMISSIONS.sales] },
  { id: "inventory", label: "Inventario", short: "IN", permissions: [PERMISSIONS.productsView] },
  { id: "customers", label: "Clientes", short: "CL", permissions: [PERMISSIONS.customersManage] },
  { id: "users", label: "Usuarios y roles", short: "UR", permissions: [PERMISSIONS.users] },
  { id: "billing", label: "Comprobantes", short: "CP", permissions: [PERMISSIONS.billingView] },
  { id: "settings", label: "Ajustes", short: "AJ", permissions: [PERMISSIONS.settings] }
];

const money = new Intl.NumberFormat("es-PE", {
  style: "currency",
  currency: "PEN",
  maximumFractionDigits: 2
});

function App() {
  const tableToken = new URLSearchParams(window.location.search).get("table");
  const [currentUser, setCurrentUser] = useState<AuthUser | null>(null);
  const [checkingSession, setCheckingSession] = useState(true);
  const [activeModule, setActiveModule] = useState<ModuleId>("overview");
  const [data, setData] = useState<ErpData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function refresh() {
    if (!currentUser) return;
    setLoading(true);
    setError(null);
    try {
      setData(await loadErpData(currentUser.permissions));
    } catch {
      setError("No fue posible conectar con el backend en el puerto 5210.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void restoreSession()
      .then(setCurrentUser)
      .finally(() => setCheckingSession(false));
  }, []);

  useEffect(() => { if (currentUser) void refresh(); else setData(null); }, [currentUser]);

  async function signOut() { await logout(); setCurrentUser(null); setActiveModule("overview"); }

  if (tableToken) return <GuestOrderingView token={tableToken} />;
  if (checkingSession) return <LoadingState />;
  if (!currentUser) return <LoginView onAuthenticated={setCurrentUser} />;

  const currentLabel = modules.find((module) => module.id === activeModule)?.label;
  const can = (permission: string) => currentUser.permissions.includes(permission);
  const visibleModules = modules.filter(module => module.permissions.some(can));

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-mark">
          <span className="brand-symbol">R</span>
          <div>
            <strong>ReviasMiUs</strong>
            <small>ERP OPERATIVO</small>
          </div>
        </div>

        <nav aria-label="Modulos principales">
          <p className="nav-caption">ESPACIO DE TRABAJO</p>
          {visibleModules.map((module) => (
            <button
              className={activeModule === module.id ? "nav-item active" : "nav-item"}
              key={module.id}
              onClick={() => setActiveModule(module.id)}
            >
              <span>{module.short}</span>
              {module.label}
            </button>
          ))}
        </nav>

        <div className="sidebar-foot">
          <div className="server-status"><i /> API conectada</div>
          <a href="http://localhost:5210/swagger" target="_blank" rel="noreferrer">
            Abrir Swagger
          </a>
        </div>
      </aside>

      <main>
        <header className="topbar">
          <div>
            <span className="eyebrow">Jueves, 20 de agosto</span>
            <h1>{currentLabel}</h1>
          </div>
          <div className="top-actions">
            <button className="refresh-button" onClick={() => void refresh()} disabled={loading}>
              {loading ? "Actualizando..." : "Actualizar datos"}
            </button>
            <div className="current-user"><div><strong>{currentUser.name}</strong><span>{currentUser.roleName}</span></div><div className="user-badge">{initials(currentUser.name)}</div></div>
            <button className="logout-button" onClick={() => void signOut()}>Salir</button>
          </div>
        </header>

        {error && (
          <section className="connection-error">
            <strong>Backend no disponible</strong>
            <span>{error}</span>
            <button onClick={() => void refresh()}>Reintentar</button>
          </section>
        )}

        {!data && !error && <LoadingState />}
        {data && activeModule === "overview" && <Overview data={data} />}
        {data && activeModule === "pos" && <PosView products={data.products} customers={data.customers} operator={currentUser} shifts={data.shifts} tables={data.tables} onSaleCompleted={refresh} />}
        {data && activeModule === "tables" && (can(PERMISSIONS.tablesManage) ? <TablesAndCashView tables={data.tables} shifts={data.shifts} users={data.users} canManage onRefresh={refresh} /> : <CashierTablesView tables={data.tables} shifts={data.shifts} canRelease={can(PERMISSIONS.tablesRelease)} canManageCash={can(PERMISSIONS.cashShifts)} onRefresh={refresh} />)}
        {data && activeModule === "kitchen" && <KitchenView tickets={data.kitchenTickets} onRefresh={refresh} />}
        {data && activeModule === "crm" && <CrmManagement leads={data.leads} onRefresh={refresh} />}
        {data && activeModule === "sales" && <SalesManagement orders={data.orders} customers={data.customers} products={data.products} onRefresh={refresh} />}
        {data && activeModule === "inventory" && (can(PERMISSIONS.productsManage) ? <InventoryManagement products={data.products} onRefresh={refresh} /> : <InventoryView products={data.products} />)}
        {data && activeModule === "customers" && <CustomersManagement customers={data.customers} canDelete={currentUser.role === "Administrator"} onRefresh={refresh} />}
        {data && activeModule === "users" && <SecureUsersView users={data.users} onRefresh={refresh} />}
        {data && activeModule === "billing" && (can(PERMISSIONS.billingCancel) ? <BillingManagement documents={data.fiscalDocuments} onRefresh={refresh} /> : <BillingView documents={data.fiscalDocuments} />)}
        {data && activeModule === "settings" && <SettingsView />}
      </main>
    </div>
  );
}

function Overview({ data }: { data: ErpData }) {
  const metrics = [
    { label: "Oportunidades", value: data.dashboard.leadCount, note: "Embudo comercial", tone: "sand" },
    { label: "Clientes activos", value: data.dashboard.customerCount, note: "Cartera total", tone: "mint" },
    { label: "Catalogo", value: data.dashboard.productCount, note: "Productos activos", tone: "blue" },
    { label: "Venta abierta", value: money.format(data.dashboard.openSalesValue), note: `${data.dashboard.confirmedOrderCount} pedidos`, tone: "coral" }
  ];

  return (
    <div className="page-content reveal">
      <section className="welcome-panel">
        <div>
          <span className="eyebrow light">CONTROL OPERATIVO</span>
          <h2>Todo el negocio,<br />en una sola mirada.</h2>
          <p>CRM, ventas e inventario conectados con datos reales del backend.</p>
        </div>
        <div className="pulse-orbit"><span>{data.dashboard.productCount + data.dashboard.customerCount}</span><small>registros base</small></div>
      </section>

      <section className="metric-grid">
        {metrics.map((metric, index) => (
          <article className={`metric-card ${metric.tone}`} style={{ animationDelay: `${index * 70}ms` }} key={metric.label}>
            <span>{metric.label}</span>
            <strong>{metric.value}</strong>
            <small>{metric.note}</small>
          </article>
        ))}
      </section>

      <section className="dashboard-grid">
        <article className="panel pipeline-panel">
          <PanelTitle title="Embudo comercial" action="CRM activo" />
          <div className="lead-list">
            {data.leads.slice(0, 4).map((lead) => (
              <div className="lead-row" key={lead.id}>
                <div className="avatar">{initials(lead.name)}</div>
                <div><strong>{lead.name}</strong><span>{lead.company}</span></div>
                <span className="stage-pill">{lead.stage}</span>
                <b>{lead.score}</b>
              </div>
            ))}
          </div>
        </article>

        <article className="panel stock-panel">
          <PanelTitle title="Inventario" action={`${data.dashboard.lowStockProductCount} alertas`} />
          <div className="stock-visual">
            <div className="stock-ring"><strong>{data.products.reduce((sum, item) => sum + item.stockQuantity, 0)}</strong><span>unidades</span></div>
          </div>
          <div className="sku-list">
            {data.dashboard.lowStockSkus.length === 0
              ? <span>Sin productos bajo minimo.</span>
              : data.dashboard.lowStockSkus.map((sku) => <span key={sku}>{sku}<b>Reponer</b></span>)}
          </div>
        </article>
      </section>
    </div>
  );
}

function CrmView({ leads }: { leads: Lead[] }) {
  return <DataPage title="Oportunidades comerciales" description="Seguimiento del embudo desde el primer contacto hasta el cierre.">
    <div className="kanban-grid">
      {["New", "Qualified", "Proposal", "Won"].map((stage) => (
        <section className="kanban-column" key={stage}>
          <header><span>{stage}</span><b>{leads.filter((lead) => lead.stage === stage).length}</b></header>
          {leads.filter((lead) => lead.stage === stage).map((lead) => (
            <article className="kanban-card" key={lead.id}>
              <small>{lead.source}</small><strong>{lead.company}</strong><span>{lead.name}</span>
              <div><i style={{ width: `${lead.score}%` }} /><b>{lead.score}</b></div>
            </article>
          ))}
        </section>
      ))}
    </div>
  </DataPage>;
}

function SalesView({ orders }: { orders: SalesOrder[] }) {
  return <DataPage title="Cotizaciones y pedidos" description="Del borrador a la confirmacion, con control automatico de existencias.">
    <Table headers={["Documento", "Cliente", "Atencion", "Estado", "Fecha", "Total"]}>
      {orders.length === 0 ? <EmptyRow columns={6} text="Todavia no hay pedidos registrados." /> : orders.map((order) => (
        <tr key={order.id}><td><strong>{order.orderNumber}</strong></td><td>{order.customerName}</td><td>{serviceLabel(order.serviceType)} {order.tableNumber ? `- ${order.tableNumber}` : ""}</td><td><span className="stage-pill">{order.status}</span></td><td>{new Date(order.createdAtUtc).toLocaleDateString("es-PE")}</td><td><strong>{money.format(order.totalAmount)}</strong></td></tr>
      ))}
    </Table>
  </DataPage>;
}

function InventoryView({ products }: { products: Product[] }) {
  return <DataPage title="Catalogo e inventario" description="Stock, precio y punto de reposicion para cada producto.">
    <Table headers={["Producto", "SKU", "Precio", "Existencia", "Estado"]}>
      {products.map((product) => (
        <tr key={product.id}><td><strong>{product.name}</strong></td><td><code>{product.sku}</code></td><td>{money.format(product.unitPrice)}</td><td>{product.stockQuantity} / min. {product.reorderPoint}</td><td><span className={product.needsReorder ? "status warning" : "status healthy"}>{product.needsReorder ? "Reponer" : "Saludable"}</span></td></tr>
      ))}
    </Table>
  </DataPage>;
}

function PosView({
  products,
  customers,
  operator,
  shifts,
  tables,
  onSaleCompleted
}: {
  products: Product[];
  customers: Customer[];
  operator: AuthUser;
  shifts: CashShift[];
  tables: RestaurantTable[];
  onSaleCompleted: () => Promise<void>;
}) {
  const [cart, setCart] = useState<Record<string, number>>({});
  const [customerId, setCustomerId] = useState(customers[0]?.id ?? "");
  const [serviceType, setServiceType] = useState<"DineIn" | "Takeaway" | "Delivery">("DineIn");
  const [tableNumber, setTableNumber] = useState("Mesa 01");
  const [activeCategory, setActiveCategory] = useState("Todos");
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethodName>("Cash");
  const [splitPayment, setSplitPayment] = useState(false);
  const [paymentAmounts, setPaymentAmounts] = useState<Record<PaymentMethodName, number>>({ Cash: 0, Card: 0, Yape: 0, Plin: 0, BankTransfer: 0 });
  const [documentType, setDocumentType] = useState<"Receipt" | "Invoice">("Receipt");
  const [customerTaxId, setCustomerTaxId] = useState("");
  const [openingCash, setOpeningCash] = useState(200);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [printableReceipt, setPrintableReceipt] = useState<PrintableReceipt | null>(null);
  const [closingShift, setClosingShift] = useState(false);
  const [closingCash, setClosingCash] = useState(0);

  const activeProducts = products.filter((product) => product.isActive);
  const activeCustomers = customers.filter((customer) => customer.isActive);
  const cartLines = activeProducts
    .filter((product) => (cart[product.id] ?? 0) > 0)
    .map((product) => ({ product, quantity: cart[product.id] }));
  const total = cartLines.reduce((sum, line) => sum + line.product.unitPrice * line.quantity, 0);
  const paymentMethods: Array<{ method: PaymentMethodName; label: string }> = [{ method: "Cash", label: "Efectivo" }, { method: "Yape", label: "Yape" }, { method: "Plin", label: "Plin" }, { method: "Card", label: "Tarjeta" }, { method: "BankTransfer", label: "Transferencia" }];
  const paymentParts = splitPayment
    ? paymentMethods.filter(item => paymentAmounts[item.method] > 0).map(item => ({ method: item.method, amount: paymentAmounts[item.method] }))
    : [{ method: paymentMethod, amount: total }];
  const allocatedTotal = paymentParts.reduce((sum, payment) => sum + payment.amount, 0);
  const paymentDifference = Math.round((total - allocatedTotal) * 100) / 100;
  const paymentIsValid = total > 0 && paymentParts.length > 0 && Math.abs(paymentDifference) < 0.01;
  const categories = ["Todos", ...Array.from(new Set(activeProducts.map((product) => product.category)))];
  const visibleProducts = activeCategory === "Todos"
    ? activeProducts
    : activeProducts.filter((product) => product.category === activeCategory);
  const cashier = operator;
  const activeShift = shifts.find((shift) => shift.userId === cashier?.id && shift.status === "Open");
  const availableTables = tables.filter((table) => table.status === "Free" || table.number === tableNumber);

  useEffect(() => {
    const selected = tables.find((table) => table.number === tableNumber);
    if (serviceType === "DineIn" && selected?.status !== "Free") {
      setTableNumber(tables.find((table) => table.status === "Free")?.number ?? "");
    }
  }, [serviceType, tableNumber, tables]);

  useEffect(() => {
    if (!activeCustomers.some((customer) => customer.id === customerId)) {
      setCustomerId(activeCustomers[0]?.id ?? "");
    }
  }, [activeCustomers, customerId]);

  function changeQuantity(product: Product, delta: number) {
    setMessage(null);
    setCart((current) => {
      const next = Math.max(0, Math.min(product.stockQuantity, (current[product.id] ?? 0) + delta));
      return { ...current, [product.id]: next };
    });
  }

  async function completeSale() {
    if (!customerId || cartLines.length === 0 || !cashier || !activeShift) return;
    setBusy(true);
    setMessage(null);
    try {
      const result = await createPosSale(
        cashier.id,
        customerId,
        cartLines.map((line) => ({ productId: line.product.id, quantity: line.quantity })),
        serviceType,
        tableNumber,
        paymentMethod,
        documentType,
        customerTaxId,
        paymentParts
      );
      setCart({});
      setMessage(`${result.fiscalDocument.number} emitido · Venta ${result.order.orderNumber} por ${money.format(result.order.totalAmount)}.`);
      void getPrintableReceipt(result.fiscalDocument.id)
        .then(setPrintableReceipt)
        .catch(() => setMessage(`${result.fiscalDocument.number} fue emitido, pero no se pudo preparar la vista de impresión.`));
      await onSaleCompleted();
    } catch (exception) {
      setMessage(exception instanceof Error ? exception.message : "No se pudo completar la venta.");
    } finally {
      setBusy(false);
    }
  }

  async function startShift() {
    if (!cashier) return;
    setBusy(true);
    try {
      await openCashShift(cashier.id, openingCash);
      await onSaleCompleted();
    } catch (exception) {
      setMessage(exception instanceof Error ? exception.message : "No se pudo abrir el turno.");
    } finally {
      setBusy(false);
    }
  }

  async function finishShift() {
    if (!activeShift) return;
    setBusy(true); setMessage(null);
    try {
      const closed = await closeCashShift(activeShift.id, closingCash);
      setClosingShift(false);
      setMessage(`Turno cerrado. Diferencia: ${money.format(closed.difference ?? 0)}.`);
      await onSaleCompleted();
    } catch (exception) {
      setMessage(exception instanceof Error ? exception.message : "No se pudo cerrar el turno.");
    } finally { setBusy(false); }
  }

  if (!activeShift) {
    return <div className="shift-gate reveal"><div><span className="eyebrow">INICIO DE JORNADA</span><h2>Abrir caja</h2><p>{cashier?.name ?? "Cajero"} debe abrir un turno antes de registrar ventas.</p><label>Fondo inicial<input type="number" min="0" value={openingCash} onChange={(event) => setOpeningCash(Number(event.target.value))} /></label>{message && <p className="pos-message">{message}</p>}<button className="charge-button" disabled={busy || !cashier} onClick={() => void startShift()}>{busy ? "Abriendo..." : `Abrir turno con ${money.format(openingCash)}`}</button></div></div>;
  }

  return <div className="pos-layout reveal">
    <section className="pos-catalog">
      <header className="pos-heading"><div><span className="eyebrow">CAJA ABIERTA</span><h2>Sabor Peruano POS</h2></div><div className="pos-shift-actions"><div className="shift-pill"><i /> Turno de {cashier.name}</div><button className="close-shift-button" onClick={() => { setClosingCash(activeShift.expectedCash); setClosingShift(true); }}>Cerrar turno</button></div></header>
      <div className="service-toolbar">
        <div className="service-types">
          {(["DineIn", "Takeaway", "Delivery"] as const).map((type) => <button className={serviceType === type ? "active" : ""} key={type} onClick={() => setServiceType(type)}>{serviceLabel(type)}</button>)}
        </div>
        {serviceType === "DineIn" && <select aria-label="Mesa" value={tableNumber} onChange={(event) => setTableNumber(event.target.value)}>{availableTables.map((table) => <option key={table.number}>{table.number}</option>)}</select>}
      </div>
      <div className="category-tabs">{categories.map((category) => <button className={activeCategory === category ? "active" : ""} key={category} onClick={() => setActiveCategory(category)}>{category}</button>)}</div>
      <div className="product-grid">
        {visibleProducts.map((product, index) => (
          <button className="product-tile" key={product.id} disabled={product.stockQuantity === 0} onClick={() => changeQuantity(product, 1)}>
            <span className={`product-art art-${index % 4}`}>{product.sku.slice(0, 2)}</span>
            <span className="product-name">{product.name}</span>
            <span className="product-meta">{product.category} · {product.stockQuantity} porciones</span>
            <strong>{money.format(product.unitPrice)}</strong>
            {(cart[product.id] ?? 0) > 0 && <b className="cart-count">{cart[product.id]}</b>}
          </button>
        ))}
      </div>
    </section>

    <aside className="checkout-panel">
      <header><div><span>{serviceLabel(serviceType)} {serviceType === "DineIn" ? `· ${tableNumber}` : ""}</span><h3>{cartLines.length} productos</h3></div><button onClick={() => setCart({})}>Limpiar</button></header>
      <label className="customer-select">Cliente<select value={customerId} onChange={(event) => setCustomerId(event.target.value)}>{activeCustomers.map((customer) => <option value={customer.id} key={customer.id}>{customer.name}</option>)}</select></label>
      <div className="cart-lines">
        {cartLines.length === 0 && <div className="empty-cart"><span>+</span><p>Selecciona productos para iniciar la venta.</p></div>}
        {cartLines.map(({ product, quantity }) => (
          <div className="cart-line" key={product.id}>
            <div><strong>{product.name}</strong><span>{money.format(product.unitPrice)} c/u</span></div>
            <div className="quantity-control"><button onClick={() => changeQuantity(product, -1)}>-</button><b>{quantity}</b><button onClick={() => changeQuantity(product, 1)}>+</button></div>
            <strong>{money.format(product.unitPrice * quantity)}</strong>
          </div>
        ))}
      </div>
      <div className="checkout-summary"><span>Subtotal <b>{money.format(total)}</b></span><span>Impuestos <b>Incluidos</b></span><div>Total <strong>{money.format(total)}</strong></div></div>
      <div className="payment-mode-heading"><strong>Forma de pago</strong><button className={splitPayment ? "active" : ""} onClick={() => { setSplitPayment(current => !current); setPaymentAmounts({ Cash: total, Card: 0, Yape: 0, Plin: 0, BankTransfer: 0 }); }}>{splitPayment ? "Usar un solo medio" : "Dividir pago"}</button></div>
      {!splitPayment ? <div className="payment-options"><label>Pago<select value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value as PaymentMethodName)}>{paymentMethods.map(item => <option key={item.method} value={item.method}>{item.label}</option>)}</select></label><label>Comprobante<select value={documentType} onChange={(event) => setDocumentType(event.target.value as typeof documentType)}><option value="Receipt">Boleta</option><option value="Invoice">Factura</option></select></label></div> : <div className="split-payment"><div className="split-payment-grid">{paymentMethods.map(item => <label key={item.method}><span>{item.label}</span><div><small>S/</small><input type="number" min="0" step="0.01" value={paymentAmounts[item.method] || ""} placeholder="0.00" onChange={event => setPaymentAmounts(current => ({ ...current, [item.method]: Math.max(0, Number(event.target.value)) }))} /></div></label>)}</div><div className={`payment-balance ${paymentIsValid ? "balanced" : "pending"}`}><span>Asignado <b>{money.format(allocatedTotal)}</b></span><strong>{paymentDifference > 0 ? `Falta ${money.format(paymentDifference)}` : paymentDifference < 0 ? `Excede ${money.format(Math.abs(paymentDifference))}` : "Pago cuadrado"}</strong></div><label className="split-document">Comprobante<select value={documentType} onChange={(event) => setDocumentType(event.target.value as typeof documentType)}><option value="Receipt">Boleta</option><option value="Invoice">Factura</option></select></label></div>}
      {documentType === "Invoice" && <label className="ruc-field">RUC<input maxLength={11} value={customerTaxId} onChange={(event) => setCustomerTaxId(event.target.value.replace(/\D/g, ""))} placeholder="11 digitos" /></label>}
      {message && <p className="pos-message">{message}</p>}
      <button className="charge-button" disabled={busy || cartLines.length === 0 || !customerId || !paymentIsValid || (documentType === "Invoice" && customerTaxId.length !== 11)} onClick={() => void completeSale()}>{busy ? "Procesando..." : `Cobrar ${money.format(total)}`}</button>
    </aside>
    {printableReceipt && <ReceiptModal receipt={printableReceipt} onClose={() => setPrintableReceipt(null)} />}
    {closingShift && <div className="modal-backdrop"><section className="editor-modal shift-close-modal" role="dialog" aria-modal="true"><header><div><span>CIERRE DE CAJA</span><h3>Finalizar turno de {cashier.name}</h3></div><button onClick={() => setClosingShift(false)}>×</button></header><div className="shift-close-body"><div className="shift-close-summary"><span>Ventas del turno<strong>{money.format(activeShift.grossSales)}</strong></span><span>Efectivo esperado<strong>{money.format(activeShift.expectedCash)}</strong></span></div><label>Efectivo contado<input autoFocus type="number" min="0" step="0.01" value={closingCash} onChange={event => setClosingCash(Math.max(0, Number(event.target.value)))} /></label><div className={`shift-difference ${closingCash === activeShift.expectedCash ? "balanced" : "warning"}`}><span>Diferencia</span><strong>{money.format(closingCash - activeShift.expectedCash)}</strong></div><p>Al confirmar se cerrará tu turno actual. Para volver a vender tendrás que abrir uno nuevo.</p><div className="form-actions"><button onClick={() => setClosingShift(false)}>Continuar vendiendo</button><button className="primary-button" disabled={busy} onClick={() => void finishShift()}>{busy ? "Cerrando..." : "Confirmar cierre"}</button></div></div></section></div>}
  </div>;
}

function UsersView({ users }: { users: UserAccount[] }) {
  const roleLabels: Record<string, string> = {
    Administrator: "Administrador",
    Cashier: "Cajero",
    Sales: "Ventas",
    Warehouse: "Almacen"
  };

  return <DataPage title="Usuarios y permisos" description="Control de acceso por funcion operativa. Las contrasenas y sesiones se incorporaran con Identity y JWT.">
    <div className="role-cards">
      <article><span>AD</span><div><strong>Administrador</strong><small>Configuracion y acceso total</small></div></article>
      <article><span>CJ</span><div><strong>Cajero</strong><small>Punto de venta y cierre de caja</small></div></article>
      <article><span>VT</span><div><strong>Ventas</strong><small>CRM, clientes y cotizaciones</small></div></article>
      <article><span>AL</span><div><strong>Almacen</strong><small>Productos y movimientos de stock</small></div></article>
    </div>
    <Table headers={["Usuario", "Correo", "Rol", "Estado", "Creado"]}>
      {users.map((user) => <tr key={user.id}><td><div className="user-cell"><span>{initials(user.name)}</span><strong>{user.name}</strong></div></td><td>{user.email}</td><td><span className="role-pill">{roleLabels[user.role] ?? user.role}</span></td><td><span className={user.isActive ? "status healthy" : "status warning"}>{user.isActive ? "Activo" : "Inactivo"}</span></td><td>{new Date(user.createdAtUtc).toLocaleDateString("es-PE")}</td></tr>)}
    </Table>
  </DataPage>;
}

function CashierTablesView({ tables, shifts, canRelease, canManageCash, onRefresh }: { tables: RestaurantTable[]; shifts: CashShift[]; canRelease: boolean; canManageCash: boolean; onRefresh: () => Promise<void> }) {
  const [countedCash, setCountedCash] = useState<Record<string, number>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const openShifts = shifts.filter(shift => shift.status === "Open");
  async function close(shift: CashShift) { setBusy(shift.id); try { await closeCashShift(shift.id, countedCash[shift.id] ?? shift.expectedCash); await onRefresh(); } finally { setBusy(null); } }
  async function release(number: string) { setBusy(number); try { await releaseTable(number); await onRefresh(); } finally { setBusy(null); } }
  return <div className="page-content reveal"><section className="section-heading"><div><span className="eyebrow">OPERACION DE SALON</span><h2>Mesas y turno</h2><p>Las acciones disponibles dependen de los permisos asignados al rol.</p></div></section><div className="operations-grid"><section className="panel"><PanelTitle title="Plano del restaurante" action={`${tables.filter(table => table.status === "Occupied").length} ocupadas`} /><div className="floor-map">{tables.map(table => <article className={`restaurant-table ${table.status.toLowerCase()}`} key={table.number}><span>{table.area}</span><strong>{table.number}</strong><small>{table.seats} asientos · {tableStatusLabel(table.status)}</small>{canRelease && table.status === "Cleaning" && <div className="table-actions"><button disabled={busy === table.number} onClick={() => void release(table.number)}>Liberar</button></div>}</article>)}</div></section>{canManageCash && <section className="panel shifts-panel"><PanelTitle title="Mi turno de caja" action={`${openShifts.length} abierto`} />{openShifts.map(shift => <article className="shift-card" key={shift.id}><div><span>CAJA ABIERTA</span><strong>{shift.userName}</strong><small>{shift.terminal}</small></div><dl><div><dt>Ventas</dt><dd>{money.format(shift.grossSales)}</dd></div><div><dt>Efectivo esperado</dt><dd>{money.format(shift.expectedCash)}</dd></div></dl><label>Efectivo contado<input type="number" value={countedCash[shift.id] ?? shift.expectedCash} onChange={event => setCountedCash(current => ({ ...current, [shift.id]: Number(event.target.value) }))} /></label><button disabled={busy === shift.id} onClick={() => void close(shift)}>Cerrar y cuadrar caja</button></article>)}</section>}</div></div>;
}

function TablesAndCashView({ tables, shifts, users, canManage, onRefresh }: { tables: RestaurantTable[]; shifts: CashShift[]; users: UserAccount[]; canManage: boolean; onRefresh: () => Promise<void> }) {
  const [countedCash, setCountedCash] = useState<Record<string, number>>({});
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ original?: string; number: string; area: string; seats: number } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [qrTable, setQrTable] = useState<RestaurantTable | null>(null);
  const openShifts = shifts.filter((shift) => shift.status === "Open");

  async function close(shift: CashShift) {
    setBusyId(shift.id);
    try {
      await closeCashShift(shift.id, countedCash[shift.id] ?? shift.expectedCash);
      await onRefresh();
    } finally {
      setBusyId(null);
    }
  }

  async function freeTable(number: string) {
    setBusyId(number);
    try {
      await releaseTable(number);
      await onRefresh();
    } finally {
      setBusyId(null);
    }
  }

  async function saveTable(event: React.FormEvent) {
    event.preventDefault();
    if (!editor) return;
    setBusyId(editor.original ?? "new");
    try {
      const body = { number: editor.number, area: editor.area, seats: editor.seats };
      if (editor.original) await updateTable(editor.original, body); else await createTable(body);
      setEditor(null); setNotice("Mesa guardada correctamente."); await onRefresh();
    } catch (error) { setNotice(error instanceof Error ? error.message : "No se pudo guardar la mesa."); }
    finally { setBusyId(null); }
  }

  async function removeTable(table: RestaurantTable) {
    if (!window.confirm(`¿Eliminar ${table.number}?`)) return;
    try { await deleteTable(table.number); await onRefresh(); setNotice("Mesa eliminada correctamente."); }
    catch (error) { setNotice(error instanceof Error ? error.message : "No se pudo eliminar la mesa."); }
  }

  return <div className="page-content reveal"><section className="section-heading"><div><span className="eyebrow">OPERACION EN TIEMPO REAL</span><h2>Mesas, QR y caja</h2><p>Plano tactil, pedidos digitales por mesa y control de los turnos activos.</p></div><button className="primary-button" onClick={() => setEditor({ number: `Mesa ${String(tables.length + 1).padStart(2, "0")}`, area: "Salon principal", seats: 4 })}>Nueva mesa</button></section>{notice && <p className="management-notice">{notice}</p>}<div className="operations-grid"><section className="panel"><PanelTitle title="Plano del restaurante" action={`${tables.filter((table) => table.status === "Occupied").length} ocupadas`} /><div className="floor-map">{tables.map((table) => <article className={`restaurant-table ${table.status.toLowerCase()}`} key={table.number}><span>{table.area}</span><strong>{table.number}</strong><small>{table.seats} asientos · {tableStatusLabel(table.status)}</small><div className="table-actions"><button className="qr-action" onClick={() => setQrTable(table)}>QR</button>{table.status === "Cleaning" && <button disabled={busyId === table.number} onClick={() => void freeTable(table.number)}>Liberar</button>}{table.status === "Free" && <><button onClick={() => setEditor({ original: table.number, number: table.number, area: table.area, seats: table.seats })}>Editar</button><button className="danger" onClick={() => void removeTable(table)}>Eliminar</button></>}</div></article>)}</div></section><section className="panel shifts-panel"><PanelTitle title="Turnos de caja" action={`${openShifts.length} abiertos`} />{openShifts.length === 0 && <p className="empty-note">No hay turnos abiertos. El cajero puede abrir uno desde Punto de venta.</p>}{openShifts.map((shift) => <article className="shift-card" key={shift.id}><div><span>CAJA ABIERTA</span><strong>{users.find((user) => user.id === shift.userId)?.name ?? shift.userName}</strong><small>{shift.terminal} · {new Date(shift.openedAtUtc).toLocaleTimeString("es-PE", { hour: "2-digit", minute: "2-digit" })}</small></div><dl><div><dt>Ventas</dt><dd>{money.format(shift.grossSales)}</dd></div><div><dt>Efectivo esperado</dt><dd>{money.format(shift.expectedCash)}</dd></div></dl><label>Efectivo contado<input type="number" value={countedCash[shift.id] ?? shift.expectedCash} onChange={(event) => setCountedCash((current) => ({ ...current, [shift.id]: Number(event.target.value) }))} /></label><button disabled={busyId === shift.id} onClick={() => void close(shift)}>Cerrar y cuadrar caja</button></article>)}</section></div>{qrTable && <TableQrModal table={qrTable} onClose={() => setQrTable(null)} onRenewed={onRefresh} />}{editor && <div className="modal-backdrop"><section className="editor-modal" role="dialog" aria-modal="true"><header><div><span>PLANO DEL RESTAURANTE</span><h3>{editor.original ? "Editar mesa" : "Nueva mesa"}</h3></div><button onClick={() => setEditor(null)}>×</button></header><form className="editor-form" onSubmit={saveTable}><label>Numero<input required value={editor.number} onChange={event => setEditor({ ...editor, number: event.target.value })} /></label><label>Area<input required value={editor.area} onChange={event => setEditor({ ...editor, area: event.target.value })} /></label><label>Asientos<input required type="number" min="1" value={editor.seats} onChange={event => setEditor({ ...editor, seats: Number(event.target.value) })} /></label><div className="form-actions"><button type="button" onClick={() => setEditor(null)}>Cancelar</button><button className="primary-button" disabled={busyId !== null}>Guardar mesa</button></div></form></section></div>}</div>;
}

function KitchenView({ tickets, onRefresh }: { tickets: KitchenTicket[]; onRefresh: () => Promise<void> }) {
  const [busyId, setBusyId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const stages = ["Pending", "Preparing", "Ready", "Delivered", "Cancelled"];
  async function advance(ticketId: string) {
    setBusyId(ticketId);
    try {
      await advanceKitchenTicket(ticketId);
      await onRefresh();
    } finally {
      setBusyId(null);
    }
  }
  async function cancel(ticketId: string) { if (!window.confirm("¿Cancelar esta comanda?")) return; setBusyId(ticketId); try { await cancelKitchenTicket(ticketId); await onRefresh(); setNotice("Comanda cancelada."); } catch (error) { setNotice(error instanceof Error ? error.message : "No se pudo cancelar."); } finally { setBusyId(null); } }
  return <DataPage title="Pantalla de cocina" description="Comandas ordenadas por etapa y tiempo de espera.">{notice && <p className="management-notice">{notice}</p>}<div className="kitchen-board">{stages.map((stage) => <section className={`kitchen-column kitchen-${stage.toLowerCase()}`} key={stage}><header><span>{kitchenStatusLabel(stage)}</span><b>{tickets.filter((ticket) => ticket.status === stage).length}</b></header>{tickets.filter((ticket) => ticket.status === stage).map((ticket) => <article key={ticket.id}><small>{ticket.tableNumber ?? "Para llevar"}</small><strong>{ticket.orderNumber}</strong><span>{minutesAgo(ticket.createdAtUtc)} min esperando</span><ul>{ticket.lines.map(line => <li key={line.productName}><b>{line.quantity}x</b> {line.productName}</li>)}</ul>{stage !== "Delivered" && stage !== "Cancelled" && <div className="kitchen-actions"><button disabled={busyId === ticket.id} onClick={() => void advance(ticket.id)}>{stage === "Pending" ? "Empezar" : stage === "Preparing" ? "Marcar listo" : "Entregar"}</button><button className="danger" disabled={busyId === ticket.id} onClick={() => void cancel(ticket.id)}>Cancelar</button></div>}</article>)}</section>)}</div></DataPage>;
}

function BillingView({ documents }: { documents: FiscalDocument[] }) {
  return <DataPage title="Comprobantes" description="Boletas y facturas internas listas para enviarse mediante el futuro adaptador SUNAT."><div className="billing-summary"><article><span>Documentos</span><strong>{documents.length}</strong></article><article><span>Total emitido</span><strong>{money.format(documents.reduce((sum, item) => sum + item.totalAmount, 0))}</strong></article><article><span>IGV incluido</span><strong>{money.format(documents.reduce((sum, item) => sum + item.taxAmount, 0))}</strong></article></div><Table headers={["Numero", "Tipo", "Cliente", "RUC", "Base", "IGV", "Total", "Estado"]}>{documents.length === 0 ? <EmptyRow columns={8} text="Aun no se emitieron comprobantes." /> : documents.map((document) => <tr key={document.id}><td><strong>{document.number}</strong></td><td>{document.type === "Invoice" ? "Factura" : "Boleta"}</td><td>{document.customerName}</td><td>{document.customerTaxId ?? "-"}</td><td>{money.format(document.taxableAmount)}</td><td>{money.format(document.taxAmount)}</td><td><strong>{money.format(document.totalAmount)}</strong></td><td><span className="status healthy">{document.status}</span></td></tr>)}</Table></DataPage>;
}

function CustomersView({ count }: { count: number }) {
  return <DataPage title="Directorio de clientes" description="La vista detallada se conectara al siguiente bloque del modulo de clientes."><div className="coming-card"><span>{count}</span><h3>clientes activos</h3><p>La API ya contiene el directorio. El formulario visual de alta y edicion sera el siguiente paso.</p></div></DataPage>;
}

function DataPage({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return <div className="page-content reveal"><section className="section-heading"><div><span className="eyebrow">MODULO OPERATIVO</span><h2>{title}</h2><p>{description}</p></div></section>{children}</div>;
}

function Table({ headers, children }: { headers: string[]; children: React.ReactNode }) {
  return <div className="table-wrap"><table><thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{children}</tbody></table></div>;
}

function EmptyRow({ columns, text }: { columns: number; text: string }) {
  return <tr><td className="empty-cell" colSpan={columns}>{text}</td></tr>;
}

function PanelTitle({ title, action }: { title: string; action: string }) {
  return <header className="panel-title"><h3>{title}</h3><span>{action}</span></header>;
}

function LoadingState() {
  return <div className="loading-state"><span /><span /><span /><p>Sincronizando modulos...</p></div>;
}

function initials(name: string) {
  return name.split(" ").slice(0, 2).map((part) => part[0]).join("");
}

function roleLabel(role: string) {
  return ({ Administrator: "Administrador", Cashier: "Cajero", Sales: "Ventas", Warehouse: "Almacen" } as Record<string, string>)[role] ?? role;
}

function serviceLabel(serviceType: string) {
  return ({ DineIn: "Salon", Takeaway: "Para llevar", Delivery: "Delivery", Retail: "Mostrador" } as Record<string, string>)[serviceType] ?? serviceType;
}

function tableStatusLabel(status: string) {
  return ({ Free: "Libre", Reserved: "Reservada", Occupied: "Ocupada", Cleaning: "Limpieza" } as Record<string, string>)[status] ?? status;
}

function kitchenStatusLabel(status: string) {
  return ({ Pending: "Por preparar", Preparing: "Preparando", Ready: "Listo", Delivered: "Entregado", Cancelled: "Cancelado" } as Record<string, string>)[status] ?? status;
}

function minutesAgo(date: string) {
  return Math.max(0, Math.floor((Date.now() - new Date(date).getTime()) / 60000));
}

export default App;
