import { useEffect, useState, type FormEvent } from "react";
import { createRole, createUser, deleteRole, deleteUser, getPermissions, getRoles, getSettings, login, updateRole, updateSettings, updateUser, type AuthUser, type PermissionDefinition, type ReceiptTemplate, type RoleDefinition, type SystemSettings, type UserAccount } from "./api";
import { receiptFromPreview, ThermalReceipt } from "./ReceiptViews";

export function LoginView({ onAuthenticated }: { onAuthenticated: (user: AuthUser) => void }) {
  const [email, setEmail] = useState("admin@revias.local");
  const [password, setPassword] = useState("Admin123!");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null);
    try { onAuthenticated(await login(email, password)); }
    catch (exception) { setError(exception instanceof Error ? exception.message : "No se pudo iniciar sesion."); }
    finally { setBusy(false); }
  }
  return <main className="login-page"><section className="login-visual"><span className="login-brand">R</span><div><small>REVIASMIUS · ERP GASTRONOMICO</small><h1>Tu operacion empieza con una identidad segura.</h1><p>Cada venta, cambio de precio y ajuste queda separado por responsabilidad.</p></div><div className="security-points"><span>Token de acceso</span><span>Permisos por rol</span><span>Sesion renovable</span></div></section><section className="login-panel"><form onSubmit={submit}><span className="eyebrow">ACCESO AL SISTEMA</span><h2>Iniciar sesion</h2><p>Ingresa con la cuenta asignada para tu turno.</p><label>Correo<input type="email" required autoComplete="username" value={email} onChange={event => setEmail(event.target.value)} /></label><label>Contraseña<input type="password" required autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} /></label>{error && <p className="login-error">{error}</p>}<button disabled={busy}>{busy ? "Verificando..." : "Ingresar de forma segura"}</button><div className="demo-access"><strong>Accesos de prueba</strong><span>Admin: admin@revias.local / Admin123!</span><span>Cajero: caja@revias.local / Caja123!</span></div></form></section></main>;
}

export function SettingsView() {
  const [settings, setSettings] = useState<SystemSettings | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  useEffect(() => { void getSettings().then(setSettings).catch(error => setMessage(error instanceof Error ? error.message : "No se pudieron cargar los ajustes.")); }, []);
  async function submit(event: FormEvent) {
    event.preventDefault(); if (!settings) return; setBusy(true); setMessage(null);
    try { setSettings(await updateSettings(settings)); setMessage("Ajustes guardados correctamente."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "No se pudieron guardar los ajustes."); }
    finally { setBusy(false); }
  }
  function uploadLogo(file?: File) {
    if (!file) return;
    if (!file.type.startsWith("image/")) { setMessage("Selecciona una imagen PNG, JPG o WEBP."); return; }
    if (file.size > 375_000) { setMessage("El logo debe pesar menos de 375 KB para no ralentizar la caja."); return; }
    const reader = new FileReader();
    reader.onload = () => setSettings(current => current ? { ...current, receiptLogoDataUrl: String(reader.result) } : current);
    reader.readAsDataURL(file);
  }
  if (!settings) return <div className="loading-state"><span /><span /><span /><p>Cargando ajustes...</p></div>;
  const template: ReceiptTemplate = { businessName: settings.businessName, taxId: settings.taxId, currency: settings.currency, taxRate: settings.taxRate, logoDataUrl: settings.receiptLogoDataUrl, paperWidthMm: settings.receiptPaperWidthMm, alignment: settings.receiptAlignment, density: settings.receiptDensity, headerText: settings.receiptHeaderText, footerText: settings.receiptFooterText, showBusinessName: settings.receiptShowBusinessName, showTaxId: settings.receiptShowTaxId, showCashier: settings.receiptShowCashier, showCustomer: settings.receiptShowCustomer, showPaymentMethod: settings.receiptShowPaymentMethod, showQrPlaceholder: settings.receiptShowQrPlaceholder };
  const toggle = (key: keyof SystemSettings) => setSettings({ ...settings, [key]: !settings[key] });
  const options: Array<[keyof SystemSettings, string]> = [["receiptShowBusinessName", "Nombre del negocio"], ["receiptShowTaxId", "RUC"], ["receiptShowCashier", "Cajero"], ["receiptShowCustomer", "Cliente"], ["receiptShowPaymentMethod", "Forma de pago"], ["receiptShowQrPlaceholder", "QR del comprobante"]];

  return <div className="page-content reveal"><section className="section-heading"><div><span className="eyebrow">SOLO ADMINISTRADOR</span><h2>Ajustes y diseñador de tickets</h2><p>Identidad, facturación y apariencia del comprobante térmico.</p></div></section>{message && <p className="management-notice">{message}</p>}<form className="settings-grid" onSubmit={submit}>
    <section><header><span>01</span><div><h3>Empresa</h3><p>Identidad del negocio.</p></div></header><label>Nombre comercial<input required value={settings.businessName} onChange={event => setSettings({ ...settings, businessName: event.target.value })} /></label><label>RUC emisor<input maxLength={11} value={settings.taxId} onChange={event => setSettings({ ...settings, taxId: event.target.value.replace(/\D/g, "") })} placeholder="11 dígitos" /></label><label>Zona horaria<input required value={settings.timeZone} onChange={event => setSettings({ ...settings, timeZone: event.target.value })} /></label></section>
    <section><header><span>02</span><div><h3>Facturación</h3><p>Parámetros usados al emitir.</p></div></header><label>Moneda<select value={settings.currency} onChange={event => setSettings({ ...settings, currency: event.target.value })}><option value="PEN">PEN · Sol peruano</option><option value="USD">USD · Dólar</option></select></label><label>Impuesto incluido (%)<input type="number" min="0" max="100" step="0.01" value={settings.taxRate} onChange={event => setSettings({ ...settings, taxRate: Number(event.target.value) })} /></label><div className="setting-pair"><label>Serie boleta<input required value={settings.receiptSeries} onChange={event => setSettings({ ...settings, receiptSeries: event.target.value })} /></label><label>Serie factura<input required value={settings.invoiceSeries} onChange={event => setSettings({ ...settings, invoiceSeries: event.target.value })} /></label></div><label className="setting-check"><input type="checkbox" checked={settings.requireTableForDineIn} onChange={event => setSettings({ ...settings, requireTableForDineIn: event.target.checked })} /> Exigir mesa para consumo en salón</label></section>
    <section className="ticket-designer"><header><span>03</span><div><h3>Diseñador de ticket térmico</h3><p>Personaliza y observa el resultado antes de imprimir.</p></div></header><div className="ticket-design-layout"><div className="ticket-controls">
      <div className="logo-uploader">{settings.receiptLogoDataUrl ? <img src={settings.receiptLogoDataUrl} alt="Logo actual" /> : <span>LOGO</span>}<div><strong>Logo de la empresa</strong><small>PNG, JPG o WEBP · máximo 375 KB</small><label className="upload-button">Seleccionar imagen<input type="file" accept="image/png,image/jpeg,image/webp" onChange={event => uploadLogo(event.target.files?.[0])} /></label>{settings.receiptLogoDataUrl && <button type="button" onClick={() => setSettings({ ...settings, receiptLogoDataUrl: null })}>Quitar logo</button>}</div></div>
      <div className="ticket-control-row"><label>Ancho<select value={settings.receiptPaperWidthMm} onChange={event => setSettings({ ...settings, receiptPaperWidthMm: Number(event.target.value) as 58 | 80 })}><option value={58}>58 mm</option><option value={80}>80 mm</option></select></label><label>Alineación<select value={settings.receiptAlignment} onChange={event => setSettings({ ...settings, receiptAlignment: event.target.value as "Left" | "Center" })}><option value="Center">Centrada</option><option value="Left">Izquierda</option></select></label><label>Densidad<select value={settings.receiptDensity} onChange={event => setSettings({ ...settings, receiptDensity: event.target.value as "Compact" | "Normal" | "Large" })}><option value="Compact">Compacta</option><option value="Normal">Normal</option><option value="Large">Grande</option></select></label></div>
      <label>Mensaje superior<textarea maxLength={160} value={settings.receiptHeaderText} onChange={event => setSettings({ ...settings, receiptHeaderText: event.target.value })} /></label><label>Mensaje de despedida<textarea maxLength={240} value={settings.receiptFooterText} onChange={event => setSettings({ ...settings, receiptFooterText: event.target.value })} /></label>
      <fieldset className="receipt-options"><legend>Elementos visibles</legend>{options.map(([key, label]) => <label key={key}><input type="checkbox" checked={Boolean(settings[key])} onChange={() => toggle(key)} />{label}</label>)}</fieldset>
    </div><div className="ticket-preview"><span>VISTA PREVIA EN TIEMPO REAL</span><div className="ticket-preview-stage"><ThermalReceipt receipt={receiptFromPreview(template)} preview /></div><small>La impresora convertirá el logo a escala de grises.</small></div></div></section>
    <button className="settings-save" disabled={busy}>{busy ? "Guardando..." : "Guardar diseño y ajustes"}</button>
  </form></div>;
}

export function SecureUsersView({ users, onRefresh }: { users: UserAccount[]; onRefresh: () => Promise<void> }) {
  const [section, setSection] = useState<"users" | "roles">("users");
  const [roles, setRoles] = useState<RoleDefinition[]>([]);
  const [permissionCatalog, setPermissionCatalog] = useState<PermissionDefinition[]>([]);
  const [editing, setEditing] = useState<UserAccount | "new" | null>(null);
  const [editingRole, setEditingRole] = useState<RoleDefinition | "new" | null>(null);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Cashier");
  const [active, setActive] = useState(true);
  const [password, setPassword] = useState("");
  const [roleCode, setRoleCode] = useState("");
  const [roleName, setRoleName] = useState("");
  const [roleDescription, setRoleDescription] = useState("");
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function refreshRoles() {
    const [availableRoles, availablePermissions] = await Promise.all([getRoles(), getPermissions()]);
    setRoles(availableRoles);
    setPermissionCatalog(availablePermissions);
  }

  useEffect(() => { void refreshRoles().catch(error => setMessage(error instanceof Error ? error.message : "No se pudieron cargar los roles.")); }, []);

  function openUser(user?: UserAccount) {
    setEditing(user ?? "new");
    setName(user?.name ?? "");
    setEmail(user?.email ?? "");
    setRole(user?.role ?? roles.find(item => item.code === "Cashier")?.code ?? roles[0]?.code ?? "");
    setActive(user?.isActive ?? true);
    setPassword("");
  }

  function openRole(item?: RoleDefinition) {
    setEditingRole(item ?? "new");
    setRoleCode(item?.code ?? "");
    setRoleName(item?.name ?? "");
    setRoleDescription(item?.description ?? "");
    setSelectedPermissions(item?.permissions ?? []);
  }

  async function submitUser(event: FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(null);
    try {
      if (editing === "new") await createUser({ name, email, role, password });
      else if (editing) await updateUser(editing.id, { name, email, role, isActive: active, newPassword: password || null });
      setEditing(null); await onRefresh(); await refreshRoles(); setMessage("Usuario y rol guardados correctamente.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo guardar el usuario."); }
    finally { setBusy(false); }
  }

  async function submitRole(event: FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(null);
    try {
      if (editingRole === "new") await createRole({ code: roleCode, name: roleName, description: roleDescription, permissions: selectedPermissions });
      else if (editingRole) await updateRole(editingRole.id, { name: roleName, description: roleDescription, permissions: selectedPermissions });
      setEditingRole(null); await refreshRoles(); setMessage("Rol y matriz de permisos guardados.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo guardar el rol."); }
    finally { setBusy(false); }
  }

  async function removeUser(user: UserAccount) {
    if (!window.confirm(`¿Eliminar a ${user.name}?`)) return;
    try { await deleteUser(user.id); await onRefresh(); await refreshRoles(); setMessage("Usuario eliminado."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo eliminar."); }
  }

  async function removeRole(item: RoleDefinition) {
    if (!window.confirm(`¿Eliminar el rol ${item.name}?`)) return;
    try { await deleteRole(item.id); await refreshRoles(); setMessage("Rol eliminado."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo eliminar el rol."); }
  }

  const roleLabels = Object.fromEntries(roles.map(item => [item.code, item.name]));
  const groups = [...new Set(permissionCatalog.map(item => item.group))];
  const isProtectedRole = editingRole !== "new" && editingRole?.code === "Administrator";
  const togglePermission = (code: string) => setSelectedPermissions(current => current.includes(code) ? current.filter(item => item !== code) : [...current, code]);

  return <div className="page-content reveal">
    <section className="section-heading"><div><span className="eyebrow">SEGURIDAD CENTRALIZADA</span><h2>Usuarios y roles</h2><p>Define exactamente qué puede consultar y operar cada perfil.</p></div><button className="primary-button" onClick={() => section === "users" ? openUser() : openRole()}>{section === "users" ? "Nuevo usuario" : "Nuevo rol"}</button></section>
    <div className="security-tabs"><button className={section === "users" ? "active" : ""} onClick={() => setSection("users")}>Usuarios <span>{users.length}</span></button><button className={section === "roles" ? "active" : ""} onClick={() => setSection("roles")}>Roles y permisos <span>{roles.length}</span></button></div>
    {message && <p className="management-notice">{message}</p>}
    {section === "users" && <div className="table-wrap"><table><thead><tr><th>Usuario</th><th>Correo</th><th>Rol</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>{users.map(user => <tr key={user.id}><td><strong>{user.name}</strong></td><td>{user.email}</td><td><span className="role-pill">{roleLabels[user.role] ?? user.role}</span></td><td><span className={user.isActive ? "status healthy" : "status warning"}>{user.isActive ? "Activo" : "Bloqueado"}</span></td><td><div className="row-actions"><button onClick={() => openUser(user)}>Editar</button><button className="danger" onClick={() => void removeUser(user)}>Eliminar</button></div></td></tr>)}</tbody></table></div>}
    {section === "roles" && <div className="role-definition-grid">{roles.map(item => <article key={item.id} className={item.code === "Administrator" ? "protected" : ""}><header><div><span>{item.code === "Administrator" ? "ROL PROTEGIDO" : item.isSystem ? "ROL DEL SISTEMA" : "ROL PERSONALIZADO"}</span><h3>{item.name}</h3><code>{item.code}</code></div><b>{item.userCount} usuarios</b></header><p>{item.description || "Sin descripcion."}</p><div className="permission-summary">{item.permissions.slice(0, 6).map(permission => <span key={permission}>{permissionCatalog.find(entry => entry.code === permission)?.name ?? permission}</span>)}{item.permissions.length > 6 && <span>+{item.permissions.length - 6} más</span>}</div><footer><strong>{item.permissions.length} permisos</strong><div className="row-actions"><button onClick={() => openRole(item)}>Ver{item.code === "Administrator" ? "" : " / editar"}</button>{!item.isSystem && <button className="danger" onClick={() => void removeRole(item)}>Eliminar</button>}</div></footer></article>)}</div>}
    {editing && <div className="modal-backdrop"><section className="editor-modal" role="dialog" aria-modal="true"><header><div><span>SEGURIDAD Y ACCESO</span><h3>{editing === "new" ? "Nuevo usuario" : "Editar usuario"}</h3></div><button onClick={() => setEditing(null)}>×</button></header><form className="editor-form" onSubmit={submitUser}><label>Nombre<input required value={name} onChange={event => setName(event.target.value)} /></label><label>Correo<input type="email" required value={email} onChange={event => setEmail(event.target.value)} /></label><label>Rol<select required value={role} onChange={event => setRole(event.target.value)}>{roles.map(item => <option value={item.code} key={item.id}>{item.name}</option>)}</select></label><label>{editing === "new" ? "Contraseña inicial" : "Nueva contraseña (opcional)"}<input type="password" required={editing === "new"} minLength={8} value={password} onChange={event => setPassword(event.target.value)} /></label>{editing !== "new" && <label className="check-field"><input type="checkbox" checked={active} onChange={event => setActive(event.target.checked)} /> Usuario activo</label>}<div className="form-actions"><button type="button" onClick={() => setEditing(null)}>Cancelar</button><button className="primary-button" disabled={busy || !role}>{busy ? "Guardando..." : "Guardar usuario"}</button></div></form></section></div>}
    {editingRole && <div className="modal-backdrop"><section className="editor-modal role-editor" role="dialog" aria-modal="true"><header><div><span>MATRIZ DE AUTORIZACION</span><h3>{editingRole === "new" ? "Crear rol" : `Rol: ${roleName}`}</h3></div><button onClick={() => setEditingRole(null)}>×</button></header><form className="role-form" onSubmit={submitRole}><div className="role-basics"><label>Nombre visible<input required value={roleName} disabled={isProtectedRole} onChange={event => setRoleName(event.target.value)} /></label><label>Codigo estable<input required minLength={3} pattern="[A-Za-z0-9_-]+" value={roleCode} disabled={editingRole !== "new"} onChange={event => setRoleCode(event.target.value.replace(/\s+/g, ""))} /></label><label>Descripcion<textarea value={roleDescription} disabled={isProtectedRole} onChange={event => setRoleDescription(event.target.value)} /></label></div>{isProtectedRole && <p className="protected-note">El rol Administrador conserva todos los permisos críticos y no puede modificarse.</p>}<div className="permission-groups">{groups.map(group => <fieldset key={group}><legend>{group}</legend>{permissionCatalog.filter(item => item.group === group).map(permission => <label className={permission.administratorOnly ? "critical" : ""} key={permission.code}><input type="checkbox" checked={selectedPermissions.includes(permission.code)} disabled={isProtectedRole || permission.administratorOnly} onChange={() => togglePermission(permission.code)} /><span><strong>{permission.name}</strong><small>{permission.description}</small>{permission.administratorOnly && <em>Solo Administrador</em>}</span></label>)}</fieldset>)}</div><div className="form-actions"><span>{selectedPermissions.length} permisos seleccionados</span><button type="button" onClick={() => setEditingRole(null)}>Cancelar</button><button className="primary-button" disabled={busy || isProtectedRole}>{busy ? "Guardando..." : "Guardar rol"}</button></div></form></section></div>}
  </div>;
}
