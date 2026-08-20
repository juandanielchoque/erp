import { useState, type FormEvent, type ReactNode } from "react";
import {
  cancelFiscalDocument,
  cancelSalesOrder,
  createCustomer,
  createLead,
  createProduct,
  createSalesOrder,
  createUser,
  deleteCustomer,
  deleteLead,
  deleteProduct,
  deleteSalesOrder,
  deleteUser,
  getPrintableReceipt,
  updateCustomer,
  updateLead,
  updateProduct,
  updateSalesOrder,
  updateUser,
  type Customer,
  type FiscalDocument,
  type Lead,
  type Product,
  type PrintableReceipt,
  type SalesOrder,
  type UserAccount
} from "./api";
import { ReceiptModal } from "./ReceiptViews";

const money = new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" });
type Refresh = () => Promise<void>;

function errorText(error: unknown) {
  return error instanceof Error ? error.message : "No se pudo completar la operacion.";
}

function Header({ title, description, onNew, newLabel = "Nuevo registro" }: { title: string; description: string; onNew?: () => void; newLabel?: string }) {
  return <section className="section-heading"><div><span className="eyebrow">MODULO OPERATIVO</span><h2>{title}</h2><p>{description}</p></div>{onNew && <button className="primary-button" onClick={onNew}>{newLabel}</button>}</section>;
}

function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return <div className="modal-backdrop" role="presentation"><section className="editor-modal" role="dialog" aria-modal="true" aria-label={title}><header><div><span>GESTION DE DATOS</span><h3>{title}</h3></div><button type="button" onClick={onClose} aria-label="Cerrar">×</button></header>{children}</section></div>;
}

function Actions({ onEdit, onDelete, deleteLabel = "Eliminar" }: { onEdit?: () => void; onDelete?: () => void; deleteLabel?: string }) {
  return <div className="row-actions">{onEdit && <button onClick={onEdit}>Editar</button>}{onDelete && <button className="danger" onClick={onDelete}>{deleteLabel}</button>}</div>;
}

function Notice({ text }: { text: string | null }) {
  return text ? <p className="management-notice">{text}</p> : null;
}

async function removeWithConfirmation(label: string, action: () => Promise<void>, refresh: Refresh, setNotice: (text: string | null) => void) {
  if (!window.confirm(`¿Confirmas la eliminacion de ${label}?`)) return;
  try { await action(); await refresh(); setNotice("Registro eliminado correctamente."); }
  catch (error) { setNotice(errorText(error)); }
}

export function CrmManagement({ leads, onRefresh }: { leads: Lead[]; onRefresh: Refresh }) {
  const [editing, setEditing] = useState<Partial<Lead> | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const empty = { name: "", company: "", email: "", phone: "", source: "Referido", score: 50, stage: "New" };
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setNotice(null);
    try {
      const body = { name: editing?.name ?? "", company: editing?.company ?? "", email: editing?.email ?? "", phone: editing?.phone ?? null, source: editing?.source ?? "", score: Number(editing?.score ?? 0), stage: editing?.stage ?? "New" };
      if (editing?.id) await updateLead(editing.id, body); else await createLead(body);
      setEditing(null); await onRefresh(); setNotice("Oportunidad guardada correctamente.");
    } catch (error) { setNotice(errorText(error)); } finally { setBusy(false); }
  }
  return <div className="page-content reveal"><Header title="Oportunidades comerciales" description="Crea, edita, mueve o elimina oportunidades del embudo." onNew={() => setEditing(empty)} /><Notice text={notice} /><div className="kanban-grid">{["New", "Qualified", "Proposal", "Won", "Lost"].map(stage => <section className="kanban-column" key={stage}><header><span>{stage}</span><b>{leads.filter(item => item.stage === stage).length}</b></header>{leads.filter(item => item.stage === stage).map(lead => <article className="kanban-card" key={lead.id}><small>{lead.source}</small><strong>{lead.company}</strong><span>{lead.name}</span><div><i style={{ width: `${lead.score}%` }} /><b>{lead.score}</b></div><Actions onEdit={() => setEditing(lead)} onDelete={() => void removeWithConfirmation(lead.name, () => deleteLead(lead.id), onRefresh, setNotice)} /></article>)}</section>)}</div>{editing && <Modal title={editing.id ? "Editar oportunidad" : "Nueva oportunidad"} onClose={() => setEditing(null)}><form className="editor-form" onSubmit={submit}><label>Nombre<input required value={editing.name ?? ""} onChange={e => setEditing({ ...editing, name: e.target.value })} /></label><label>Empresa<input required value={editing.company ?? ""} onChange={e => setEditing({ ...editing, company: e.target.value })} /></label><label>Correo<input required type="email" value={editing.email ?? ""} onChange={e => setEditing({ ...editing, email: e.target.value })} /></label><label>Telefono<input value={editing.phone ?? ""} onChange={e => setEditing({ ...editing, phone: e.target.value })} /></label><label>Origen<input required value={editing.source ?? ""} onChange={e => setEditing({ ...editing, source: e.target.value })} /></label><label>Puntaje<input required type="number" min="0" max="100" value={editing.score ?? 0} onChange={e => setEditing({ ...editing, score: Number(e.target.value) })} /></label>{editing.id && <label>Etapa<select value={editing.stage} onChange={e => setEditing({ ...editing, stage: e.target.value })}>{["New", "Qualified", "Proposal", "Won", "Lost"].map(stage => <option key={stage}>{stage}</option>)}</select></label>}<FormButtons busy={busy} onClose={() => setEditing(null)} /></form></Modal>}</div>;
}

export function InventoryManagement({ products, onRefresh }: { products: Product[]; onRefresh: Refresh }) {
  const [editing, setEditing] = useState<Partial<Product> | null>(null); const [notice, setNotice] = useState<string | null>(null); const [busy, setBusy] = useState(false);
  const empty = { name: "", sku: "", category: "Fondos", unitPrice: 0, stockQuantity: 0, reorderPoint: 0, isActive: true };
  async function submit(event: FormEvent) { event.preventDefault(); setBusy(true); try { const body = { name: editing?.name ?? "", sku: editing?.sku ?? "", category: editing?.category ?? "General", unitPrice: Number(editing?.unitPrice), stockQuantity: Number(editing?.stockQuantity), reorderPoint: Number(editing?.reorderPoint), isActive: editing?.isActive ?? true }; if (editing?.id) await updateProduct(editing.id, body); else await createProduct(body); setEditing(null); await onRefresh(); setNotice("Producto guardado correctamente."); } catch (error) { setNotice(errorText(error)); } finally { setBusy(false); } }
  return <div className="page-content reveal"><Header title="Catalogo e inventario" description="Administra platos, precios, categorias y existencias." onNew={() => setEditing(empty)} newLabel="Nuevo producto" /><Notice text={notice} /><Table headers={["Producto", "SKU", "Categoria", "Precio", "Existencia", "Estado", "Acciones"]}>{products.map(product => <tr key={product.id}><td><strong>{product.name}</strong></td><td><code>{product.sku}</code></td><td>{product.category}</td><td>{money.format(product.unitPrice)}</td><td>{product.stockQuantity} / min. {product.reorderPoint}</td><td><span className={product.isActive && !product.needsReorder ? "status healthy" : "status warning"}>{!product.isActive ? "Inactivo" : product.needsReorder ? "Reponer" : "Saludable"}</span></td><td><Actions onEdit={() => setEditing(product)} onDelete={() => void removeWithConfirmation(product.name, () => deleteProduct(product.id), onRefresh, setNotice)} /></td></tr>)}</Table>{editing && <Modal title={editing.id ? "Editar producto" : "Nuevo producto"} onClose={() => setEditing(null)}><form className="editor-form" onSubmit={submit}><label>Nombre<input required value={editing.name ?? ""} onChange={e => setEditing({ ...editing, name: e.target.value })} /></label><label>SKU<input required value={editing.sku ?? ""} onChange={e => setEditing({ ...editing, sku: e.target.value })} /></label><label>Categoria<input required value={editing.category ?? ""} onChange={e => setEditing({ ...editing, category: e.target.value })} /></label><label>Precio<input required type="number" min="0" step="0.01" value={editing.unitPrice ?? 0} onChange={e => setEditing({ ...editing, unitPrice: Number(e.target.value) })} /></label><label>Stock<input required type="number" min="0" value={editing.stockQuantity ?? 0} onChange={e => setEditing({ ...editing, stockQuantity: Number(e.target.value) })} /></label><label>Punto de reposicion<input required type="number" min="0" value={editing.reorderPoint ?? 0} onChange={e => setEditing({ ...editing, reorderPoint: Number(e.target.value) })} /></label>{editing.id && <label className="check-field"><input type="checkbox" checked={editing.isActive ?? true} onChange={e => setEditing({ ...editing, isActive: e.target.checked })} /> Producto activo</label>}<FormButtons busy={busy} onClose={() => setEditing(null)} /></form></Modal>}</div>;
}

export function CustomersManagement({ customers, canDelete = false, onRefresh }: { customers: Customer[]; canDelete?: boolean; onRefresh: Refresh }) {
  const [editing, setEditing] = useState<Partial<Customer> | null>(null); const [notice, setNotice] = useState<string | null>(null); const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) { event.preventDefault(); setBusy(true); try { const body = { name: editing?.name ?? "", email: editing?.email ?? "", isActive: editing?.isActive ?? true }; if (editing?.id) await updateCustomer(editing.id, body); else await createCustomer(body); setEditing(null); await onRefresh(); setNotice("Cliente guardado correctamente."); } catch (error) { setNotice(errorText(error)); } finally { setBusy(false); } }
  return <div className="page-content reveal"><Header title="Directorio de clientes" description="Altas y edicion; la eliminacion queda reservada al administrador." onNew={() => setEditing({ name: "", email: "", isActive: true })} newLabel="Nuevo cliente" /><Notice text={notice} /><Table headers={["Cliente", "Correo", "Estado", "Acciones"]}>{customers.map(customer => <tr key={customer.id}><td><strong>{customer.name}</strong></td><td>{customer.email}</td><td><span className={customer.isActive ? "status healthy" : "status warning"}>{customer.isActive ? "Activo" : "Inactivo"}</span></td><td><Actions onEdit={() => setEditing(customer)} onDelete={canDelete ? () => void removeWithConfirmation(customer.name, () => deleteCustomer(customer.id), onRefresh, setNotice) : undefined} /></td></tr>)}</Table>{editing && <Modal title={editing.id ? "Editar cliente" : "Nuevo cliente"} onClose={() => setEditing(null)}><form className="editor-form" onSubmit={submit}><label>Nombre<input required value={editing.name ?? ""} onChange={e => setEditing({ ...editing, name: e.target.value })} /></label><label>Correo<input required type="email" value={editing.email ?? ""} onChange={e => setEditing({ ...editing, email: e.target.value })} /></label>{editing.id && <label className="check-field"><input type="checkbox" checked={editing.isActive ?? true} onChange={e => setEditing({ ...editing, isActive: e.target.checked })} /> Cliente activo</label>}<FormButtons busy={busy} onClose={() => setEditing(null)} /></form></Modal>}</div>;
}

export function UsersManagement({ users, onRefresh }: { users: UserAccount[]; onRefresh: Refresh }) {
  const [editing, setEditing] = useState<Partial<UserAccount> | null>(null); const [notice, setNotice] = useState<string | null>(null); const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) { event.preventDefault(); setBusy(true); try { const body = { name: editing?.name ?? "", email: editing?.email ?? "", role: editing?.role ?? "Cashier", isActive: editing?.isActive ?? true }; if (editing?.id) await updateUser(editing.id, body); else await createUser(body); setEditing(null); await onRefresh(); setNotice("Usuario guardado correctamente."); } catch (error) { setNotice(errorText(error)); } finally { setBusy(false); } }
  return <div className="page-content reveal"><Header title="Usuarios y permisos" description="Gestiona perfiles operativos y su estado de acceso." onNew={() => setEditing({ name: "", email: "", role: "Cashier", isActive: true })} newLabel="Nuevo usuario" /><Notice text={notice} /><Table headers={["Usuario", "Correo", "Rol", "Estado", "Creado", "Acciones"]}>{users.map(user => <tr key={user.id}><td><strong>{user.name}</strong></td><td>{user.email}</td><td><span className="role-pill">{user.role}</span></td><td><span className={user.isActive ? "status healthy" : "status warning"}>{user.isActive ? "Activo" : "Inactivo"}</span></td><td>{new Date(user.createdAtUtc).toLocaleDateString("es-PE")}</td><td><Actions onEdit={() => setEditing(user)} onDelete={() => void removeWithConfirmation(user.name, () => deleteUser(user.id), onRefresh, setNotice)} /></td></tr>)}</Table>{editing && <Modal title={editing.id ? "Editar usuario" : "Nuevo usuario"} onClose={() => setEditing(null)}><form className="editor-form" onSubmit={submit}><label>Nombre<input required value={editing.name ?? ""} onChange={e => setEditing({ ...editing, name: e.target.value })} /></label><label>Correo<input required type="email" value={editing.email ?? ""} onChange={e => setEditing({ ...editing, email: e.target.value })} /></label><label>Rol<select value={editing.role} onChange={e => setEditing({ ...editing, role: e.target.value })}>{["Administrator", "Cashier", "Sales", "Warehouse"].map(role => <option key={role}>{role}</option>)}</select></label>{editing.id && <label className="check-field"><input type="checkbox" checked={editing.isActive ?? true} onChange={e => setEditing({ ...editing, isActive: e.target.checked })} /> Usuario activo</label>}<FormButtons busy={busy} onClose={() => setEditing(null)} /></form></Modal>}</div>;
}

export function SalesManagement({ orders, customers, products, onRefresh }: { orders: SalesOrder[]; customers: Customer[]; products: Product[]; onRefresh: Refresh }) {
  const [editing, setEditing] = useState<SalesOrder | "new" | null>(null); const [customerId, setCustomerId] = useState(""); const [serviceType, setServiceType] = useState("Takeaway"); const [tableNumber, setTableNumber] = useState(""); const [quantities, setQuantities] = useState<Record<string, number>>({}); const [notice, setNotice] = useState<string | null>(null); const [busy, setBusy] = useState(false);
  function open(order?: SalesOrder) { setEditing(order ?? "new"); setCustomerId(order?.customerId ?? customers[0]?.id ?? ""); setServiceType(order?.serviceType ?? "Takeaway"); setTableNumber(order?.tableNumber ?? ""); setQuantities(Object.fromEntries(order?.lines.map(line => [line.productId, line.quantity]) ?? [])); }
  async function submit(event: FormEvent) { event.preventDefault(); setBusy(true); try { const body = { customerId, lines: Object.entries(quantities).filter(([, quantity]) => quantity > 0).map(([productId, quantity]) => ({ productId, quantity })), validUntilUtc: new Date(Date.now() + 30 * 86400000).toISOString(), serviceType, tableNumber: serviceType === "DineIn" ? tableNumber : null, notes: "Gestionado desde el modulo de ventas" }; if (editing !== "new" && editing) await updateSalesOrder(editing.id, body); else await createSalesOrder(body); setEditing(null); await onRefresh(); setNotice("Cotizacion guardada correctamente."); } catch (error) { setNotice(errorText(error)); } finally { setBusy(false); } }
  async function cancel(order: SalesOrder) { if (!window.confirm(`¿Cancelar ${order.orderNumber}? El stock sera repuesto cuando corresponda.`)) return; try { await cancelSalesOrder(order.id); await onRefresh(); setNotice("Documento cancelado correctamente."); } catch (error) { setNotice(errorText(error)); } }
  return <div className="page-content reveal"><Header title="Cotizaciones y pedidos" description="Los borradores se editan o eliminan; las ventas confirmadas se cancelan para conservar trazabilidad." onNew={() => open()} newLabel="Nueva cotizacion" /><Notice text={notice} /><Table headers={["Documento", "Cliente", "Atencion", "Estado", "Fecha", "Total", "Acciones"]}>{orders.map(order => <tr key={order.id}><td><strong>{order.orderNumber}</strong></td><td>{order.customerName}</td><td>{order.serviceType}</td><td><span className="stage-pill">{order.status}</span></td><td>{new Date(order.createdAtUtc).toLocaleDateString("es-PE")}</td><td>{money.format(order.totalAmount)}</td><td>{order.status === "Draft" ? <Actions onEdit={() => open(order)} onDelete={() => void removeWithConfirmation(order.orderNumber, () => deleteSalesOrder(order.id), onRefresh, setNotice)} /> : order.status === "Confirmed" ? <Actions onDelete={() => void cancel(order)} deleteLabel="Cancelar" /> : <span className="muted">Sin acciones</span>}</td></tr>)}</Table>{editing && <Modal title={editing === "new" ? "Nueva cotizacion" : `Editar ${editing.orderNumber}`} onClose={() => setEditing(null)}><form className="editor-form wide" onSubmit={submit}><label>Cliente<select required value={customerId} onChange={e => setCustomerId(e.target.value)}>{customers.filter(item => item.isActive).map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label><label>Atencion<select value={serviceType} onChange={e => setServiceType(e.target.value)}><option value="Retail">Mostrador</option><option value="DineIn">Salon</option><option value="Takeaway">Para llevar</option><option value="Delivery">Delivery</option></select></label>{serviceType === "DineIn" && <label>Mesa<input required value={tableNumber} onChange={e => setTableNumber(e.target.value)} /></label>}<fieldset className="order-lines"><legend>Productos y cantidades</legend>{products.filter(item => item.isActive).map(product => <label key={product.id}><span>{product.name}<small>{money.format(product.unitPrice)} · stock {product.stockQuantity}</small></span><input type="number" min="0" max={product.stockQuantity} value={quantities[product.id] ?? 0} onChange={e => setQuantities(current => ({ ...current, [product.id]: Number(e.target.value) }))} /></label>)}</fieldset><FormButtons busy={busy} onClose={() => setEditing(null)} /></form></Modal>}</div>;
}

export function BillingManagement({ documents, onRefresh }: { documents: FiscalDocument[]; onRefresh: Refresh }) {
  const [notice, setNotice] = useState<string | null>(null);
  const [receipt, setReceipt] = useState<PrintableReceipt | null>(null);
  async function cancel(document: FiscalDocument) { if (!window.confirm(`¿Anular el comprobante ${document.number}?`)) return; try { await cancelFiscalDocument(document.id); await onRefresh(); setNotice("Comprobante anulado conservando su trazabilidad."); } catch (error) { setNotice(errorText(error)); } }
  async function print(document: FiscalDocument) { try { setReceipt(await getPrintableReceipt(document.id)); } catch (error) { setNotice(errorText(error)); } }
  return <div className="page-content reveal"><Header title="Comprobantes" description="Imprime o reimprime con el diseño vigente; anula sin borrar el historial contable." /><Notice text={notice} /><div className="billing-summary"><article><span>Documentos</span><strong>{documents.length}</strong></article><article><span>Total vigente</span><strong>{money.format(documents.filter(item => item.status !== "Cancelled").reduce((sum, item) => sum + item.totalAmount, 0))}</strong></article><article><span>Anulados</span><strong>{documents.filter(item => item.status === "Cancelled").length}</strong></article></div><Table headers={["Numero", "Tipo", "Cliente", "Base", "IGV", "Total", "Estado", "Acciones"]}>{documents.map(document => <tr key={document.id}><td><strong>{document.number}</strong></td><td>{document.type === "Invoice" ? "Factura" : "Boleta"}</td><td>{document.customerName}</td><td>{money.format(document.taxableAmount)}</td><td>{money.format(document.taxAmount)}</td><td>{money.format(document.totalAmount)}</td><td><span className={document.status === "Cancelled" ? "status warning" : "status healthy"}>{document.status}</span></td><td><div className="row-actions"><button onClick={() => void print(document)}>Imprimir</button>{document.status !== "Cancelled" && <button className="danger" onClick={() => void cancel(document)}>Anular</button>}</div></td></tr>)}</Table>{receipt && <ReceiptModal receipt={receipt} onClose={() => setReceipt(null)} />}</div>;
}

function FormButtons({ busy, onClose }: { busy: boolean; onClose: () => void }) { return <div className="form-actions"><button type="button" onClick={onClose}>Cancelar</button><button className="primary-button" disabled={busy}>{busy ? "Guardando..." : "Guardar cambios"}</button></div>; }
function Table({ headers, children }: { headers: string[]; children: ReactNode }) { return <div className="table-wrap"><table><thead><tr>{headers.map(header => <th key={header}>{header}</th>)}</tr></thead><tbody>{children}</tbody></table></div>; }
