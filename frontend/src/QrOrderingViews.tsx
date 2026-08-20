import { useEffect, useState } from "react";
import QRCode from "qrcode";
import {
  createPublicTableOrder,
  getPublicTableMenu,
  regenerateTableQr,
  type PublicMenuProduct,
  type PublicTableMenu,
  type PublicTableOrder,
  type RestaurantTable
} from "./api";

const money = new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" });

export function GuestOrderingView({ token }: { token: string }) {
  const [menu, setMenu] = useState<PublicTableMenu | null>(null);
  const [cart, setCart] = useState<Record<string, number>>({});
  const [category, setCategory] = useState("Todos");
  const [guestName, setGuestName] = useState("");
  const [notes, setNotes] = useState("");
  const [order, setOrder] = useState<PublicTableOrder | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void getPublicTableMenu(token).then(setMenu).catch((reason: Error) => setError(reason.message));
  }, [token]);

  if (error && !menu) return <main className="guest-state"><span>QR</span><h1>Este codigo no esta disponible</h1><p>{error}</p></main>;
  if (!menu) return <main className="guest-state"><div className="guest-loader" /><p>Preparando la carta...</p></main>;
  if (order) return <main className="guest-success"><div className="success-mark">OK</div><span>PEDIDO RECIBIDO</span><h1>Ya estamos cocinando</h1><p>Tu pedido <strong>{order.orderNumber}</strong> fue enviado a cocina para {order.tableNumber}.</p><div><small>Total pendiente de pago</small><strong>{money.format(order.totalAmount)}</strong></div><button onClick={() => { setOrder(null); setCart({}); setNotes(""); }}>Hacer otro pedido</button></main>;

  const categories = ["Todos", ...new Set(menu.products.map(product => product.category))];
  const visibleProducts = category === "Todos" ? menu.products : menu.products.filter(product => product.category === category);
  const lines = menu.products.filter(product => (cart[product.id] ?? 0) > 0).map(product => ({ product, quantity: cart[product.id] }));
  const total = lines.reduce((sum, line) => sum + line.product.unitPrice * line.quantity, 0);

  function change(product: PublicMenuProduct, amount: number) {
    setCart(current => ({ ...current, [product.id]: Math.max(0, Math.min(20, product.availableQuantity, (current[product.id] ?? 0) + amount)) }));
  }

  async function submit() {
    if (lines.length === 0) return;
    setBusy(true); setError(null);
    try {
      setOrder(await createPublicTableOrder(token, { guestName, notes, lines: lines.map(line => ({ productId: line.product.id, quantity: line.quantity })) }));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "No se pudo enviar el pedido.");
    } finally { setBusy(false); }
  }

  return <main className="guest-ordering">
    <header className="guest-hero"><div><span>CARTA DIGITAL</span><h1>{menu.businessName}</h1><p>{menu.tableNumber} · {menu.area} · {menu.seats} personas</p></div><b>{lines.reduce((sum, line) => sum + line.quantity, 0)}</b></header>
    <nav className="guest-categories">{categories.map(item => <button key={item} className={category === item ? "active" : ""} onClick={() => setCategory(item)}>{item}</button>)}</nav>
    <section className="guest-products">{visibleProducts.map((product, index) => <article key={product.id}>
      <div className={`guest-food-art guest-art-${index % 4}`}><span>{product.category.slice(0, 2)}</span></div>
      <div className="guest-product-copy"><small>{product.category}</small><h2>{product.name}</h2><strong>{money.format(product.unitPrice)}</strong></div>
      <div className="guest-stepper"><button aria-label={`Quitar ${product.name}`} onClick={() => change(product, -1)}>-</button><b>{cart[product.id] ?? 0}</b><button aria-label={`Agregar ${product.name}`} onClick={() => change(product, 1)}>+</button></div>
    </article>)}</section>
    <section className="guest-checkout">
      <header><div><span>TU PEDIDO</span><strong>{lines.length} productos</strong></div><b>{money.format(total)}</b></header>
      {lines.length > 0 && <div className="guest-order-lines">{lines.map(line => <span key={line.product.id}>{line.quantity} x {line.product.name}<b>{money.format(line.product.unitPrice * line.quantity)}</b></span>)}</div>}
      <label>Nombre para identificarte (opcional)<input maxLength={80} value={guestName} onChange={event => setGuestName(event.target.value)} placeholder="Ej. Maria" /></label>
      <label>Indicaciones para cocina (opcional)<textarea maxLength={500} value={notes} onChange={event => setNotes(event.target.value)} placeholder="Sin cebolla, alergias, termino..." /></label>
      {error && <p className="guest-error">{error}</p>}
      <button className="guest-send" disabled={busy || lines.length === 0} onClick={() => void submit()}>{busy ? "Enviando..." : `Enviar a cocina · ${money.format(total)}`}</button>
      <small>El pago y el comprobante se solicitan al personal del restaurante.</small>
    </section>
  </main>;
}

export function TableQrModal({ table, onClose, onRenewed }: { table: RestaurantTable; onClose: () => void; onRenewed: () => Promise<void> }) {
  const [image, setImage] = useState("");
  const [busy, setBusy] = useState(false);
  const orderUrl = `${window.location.origin}/?table=${table.qrToken}`;

  useEffect(() => { void QRCode.toDataURL(orderUrl, { width: 520, margin: 2, color: { dark: "#142b32", light: "#ffffff" } }).then(setImage); }, [orderUrl]);

  async function renew() {
    if (!window.confirm("El QR anterior dejara de funcionar. ¿Deseas renovarlo?")) return;
    setBusy(true);
    try { await regenerateTableQr(table.number); await onRenewed(); onClose(); } finally { setBusy(false); }
  }

  function printQr() { window.print(); }

  return <div className="modal-backdrop qr-backdrop"><section className="qr-modal" role="dialog" aria-modal="true">
    <button className="qr-close" onClick={onClose}>×</button>
    <div className="qr-print-card"><span className="qr-brand">R</span><small>ESCANEA Y ORDENA</small><h2>{table.number}</h2><p>{table.area} · Carta digital del restaurante</p>{image && <img src={image} alt={`Codigo QR de ${table.number}`} />}<strong>Escanea con la camara de tu celular</strong><em>Tu pedido llegara directamente a cocina</em></div>
    <footer><a href={image} download={`QR-${table.number.replaceAll(" ", "-")}.png`}>Descargar PNG</a><button onClick={printQr}>Imprimir</button><button className="danger" disabled={busy} onClick={() => void renew()}>{busy ? "Renovando..." : "Renovar QR"}</button></footer>
  </section></div>;
}
