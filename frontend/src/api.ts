const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5210";
let accessToken: string | null = null;
let refreshPromise: Promise<AuthUser | null> | null = null;

export interface AuthUser { id: string; name: string; email: string; role: string; roleName: string; permissions: string[]; }
export interface AuthResponse { accessToken: string; expiresAtUtc: string; user: AuthUser; }
export interface ReceiptTemplate { businessName: string; taxId: string; currency: string; taxRate: number; logoDataUrl?: string | null; paperWidthMm: 58 | 80; alignment: "Left" | "Center"; density: "Compact" | "Normal" | "Large"; headerText: string; footerText: string; showBusinessName: boolean; showTaxId: boolean; showCashier: boolean; showCustomer: boolean; showPaymentMethod: boolean; showQrPlaceholder: boolean; }
export interface SystemSettings { businessName: string; taxId: string; currency: string; taxRate: number; timeZone: string; receiptSeries: string; invoiceSeries: string; requireTableForDineIn: boolean; receiptLogoDataUrl?: string | null; receiptPaperWidthMm: 58 | 80; receiptAlignment: "Left" | "Center"; receiptDensity: "Compact" | "Normal" | "Large"; receiptHeaderText: string; receiptFooterText: string; receiptShowBusinessName: boolean; receiptShowTaxId: boolean; receiptShowCashier: boolean; receiptShowCustomer: boolean; receiptShowPaymentMethod: boolean; receiptShowQrPlaceholder: boolean; }

export interface Dashboard {
  leadCount: number;
  customerCount: number;
  productCount: number;
  lowStockProductCount: number;
  confirmedOrderCount: number;
  openSalesValue: number;
  lowStockSkus: string[];
}

export interface Lead {
  id: string;
  name: string;
  company: string;
  stage: string;
  score: number;
  source: string;
  email: string;
  phone?: string | null;
}

export interface Product {
  id: string;
  name: string;
  sku: string;
  category: string;
  unitPrice: number;
  stockQuantity: number;
  reorderPoint: number;
  needsReorder: boolean;
  isActive: boolean;
}

export interface SalesOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  serviceType: string;
  tableNumber?: string | null;
  customerId: string;
  validUntilUtc: string;
  notes?: string | null;
  lines: Array<{ productId: string; productName: string; quantity: number; unitPrice: number; lineTotal: number }>;
}

export interface Customer {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
}

export interface UserAccount {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface RoleDefinition {
  id: string;
  code: string;
  name: string;
  description: string;
  isSystem: boolean;
  userCount: number;
  permissions: string[];
}

export interface PermissionDefinition {
  code: string;
  name: string;
  description: string;
  group: string;
  administratorOnly: boolean;
}

export const PERMISSIONS = {
  dashboard: "dashboard.view",
  productsView: "products.view",
  productsManage: "products.manage",
  customersView: "customers.view",
  customersManage: "customers.manage",
  crm: "crm.manage",
  sales: "sales.manage",
  pos: "pos.use",
  tablesView: "tables.view",
  tablesManage: "tables.manage",
  tablesRelease: "tables.release",
  cashShifts: "cash-shifts.manage",
  kitchen: "kitchen.manage",
  billingView: "billing.view",
  billingCancel: "billing.cancel",
  users: "users.manage",
  roles: "roles.manage",
  settings: "settings.manage"
} as const;

export interface RestaurantTable {
  number: string;
  area: string;
  seats: number;
  status: string;
  currentOrderId?: string | null;
  occupiedAtUtc?: string | null;
  qrToken: string;
}

export interface PublicMenuProduct { id: string; name: string; category: string; unitPrice: number; availableQuantity: number; }
export interface PublicTableMenu { businessName: string; tableNumber: string; area: string; seats: number; currency: string; products: PublicMenuProduct[]; }
export interface PublicTableOrder { orderId: string; orderNumber: string; tableNumber: string; totalAmount: number; status: string; createdAtUtc: string; }

export interface CashShift {
  id: string;
  userId: string;
  userName: string;
  terminal: string;
  status: string;
  openingCash: number;
  expectedCash: number;
  countedCash?: number | null;
  difference?: number | null;
  grossSales: number;
  openedAtUtc: string;
  closedAtUtc?: string | null;
}

export interface KitchenTicket {
  id: string;
  orderId: string;
  orderNumber: string;
  tableNumber?: string | null;
  status: string;
  createdAtUtc: string;
  lines: Array<{ productName: string; quantity: number }>;
}

export interface FiscalDocument {
  id: string;
  orderId: string;
  type: string;
  number: string;
  customerName: string;
  customerTaxId?: string | null;
  taxableAmount: number;
  taxAmount: number;
  totalAmount: number;
  status: string;
  issuedAtUtc: string;
}

export type PaymentMethodName = "Cash" | "Card" | "Yape" | "Plin" | "BankTransfer";
export interface ReceiptPayment { method: PaymentMethodName | "Pending"; amount: number; }
export interface PrintableReceipt { template: ReceiptTemplate; document: FiscalDocument; order: SalesOrder; cashierName: string; payments: ReceiptPayment[]; }

async function get<T>(path: string): Promise<T> {
  const response = await apiFetch(path);
  if (!response.ok) {
    throw new Error(`La API respondio con estado ${response.status}`);
  }
  return response.json() as Promise<T>;
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, "POST", body);
}

async function request<T>(path: string, method: "POST" | "PUT" | "PATCH" | "DELETE", body?: unknown): Promise<T> {
  const response = await apiFetch(path, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { error?: string } | null;
    throw new Error(problem?.error ?? `La API respondio con estado ${response.status}`);
  }

  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

async function apiFetch(path: string, init: RequestInit = {}, retry = true): Promise<Response> {
  const headers = new Headers(init.headers);
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  const response = await fetch(`${API_URL}${path}`, { ...init, headers, credentials: "include" });
  if (response.status === 401 && retry && accessToken && !path.startsWith("/api/auth/")) {
    const session = await restoreSession();
    if (session) return apiFetch(path, init, false);
  }
  return response;
}

export async function login(email: string, password: string) {
  const response = await fetch(`${API_URL}/api/auth/login`, { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }) });
  if (!response.ok) { const problem = await response.json().catch(() => null) as { error?: string } | null; throw new Error(problem?.error ?? "No se pudo iniciar sesion."); }
  const session = await response.json() as AuthResponse;
  accessToken = session.accessToken;
  return session.user;
}

export async function restoreSession(): Promise<AuthUser | null> {
  if (refreshPromise) return refreshPromise;
  refreshPromise = (async () => {
    try {
      const response = await fetch(`${API_URL}/api/auth/refresh`, { method: "POST", credentials: "include" });
      if (!response.ok) { accessToken = null; return null; }
      const session = await response.json() as AuthResponse;
      accessToken = session.accessToken;
      return session.user;
    } catch {
      accessToken = null;
      return null;
    }
  })();
  try { return await refreshPromise; } finally { refreshPromise = null; }
}

export async function logout() {
  await apiFetch("/api/auth/logout", { method: "POST" }, false).catch(() => undefined);
  accessToken = null;
}

export const createLead = (body: Omit<Lead, "id" | "stage"> & { stage?: string }) => post<Lead>("/api/crm/leads", body);
export const updateLead = (id: string, body: Omit<Lead, "id">) => request<Lead>(`/api/crm/leads/${id}`, "PUT", body);
export const deleteLead = (id: string) => request<void>(`/api/crm/leads/${id}`, "DELETE");
export const createCustomer = (body: Pick<Customer, "name" | "email">) => post<Customer>("/api/customers", body);
export const updateCustomer = (id: string, body: Pick<Customer, "name" | "email" | "isActive">) => request<Customer>(`/api/customers/${id}`, "PUT", body);
export const deleteCustomer = (id: string) => request<void>(`/api/customers/${id}`, "DELETE");
export const createProduct = (body: Omit<Product, "id" | "needsReorder" | "isActive">) => post<Product>("/api/products", body);
export const updateProduct = (id: string, body: Omit<Product, "id" | "needsReorder">) => request<Product>(`/api/products/${id}`, "PUT", body);
export const deleteProduct = (id: string) => request<void>(`/api/products/${id}`, "DELETE");
export const createUser = (body: Pick<UserAccount, "name" | "email" | "role"> & { password?: string }) => post<UserAccount>("/api/users", { ...body, password: body.password ?? "Temporal123!" });
export const updateUser = (id: string, body: Pick<UserAccount, "name" | "email" | "role" | "isActive"> & { newPassword?: string | null }) => request<UserAccount>(`/api/users/${id}`, "PUT", body);
export const deleteUser = (id: string) => request<void>(`/api/users/${id}`, "DELETE");
export const getRoles = () => get<RoleDefinition[]>("/api/roles");
export const getPermissions = () => get<PermissionDefinition[]>("/api/roles/permissions");
export const createRole = (body: Pick<RoleDefinition, "code" | "name" | "description" | "permissions">) => post<RoleDefinition>("/api/roles", body);
export const updateRole = (id: string, body: Pick<RoleDefinition, "name" | "description" | "permissions">) => request<RoleDefinition>(`/api/roles/${id}`, "PUT", body);
export const deleteRole = (id: string) => request<void>(`/api/roles/${id}`, "DELETE");
export const createTable = (body: Pick<RestaurantTable, "number" | "area" | "seats">) => post<RestaurantTable>("/api/restaurant/tables", body);
export const updateTable = (currentNumber: string, body: Pick<RestaurantTable, "number" | "area" | "seats">) => request<RestaurantTable>(`/api/restaurant/tables/${encodeURIComponent(currentNumber)}`, "PUT", body);
export const deleteTable = (number: string) => request<void>(`/api/restaurant/tables/${encodeURIComponent(number)}`, "DELETE");
export const regenerateTableQr = (number: string) => post<RestaurantTable>(`/api/restaurant/tables/${encodeURIComponent(number)}/qr/regenerate`);
export const updateSalesOrder = (id: string, body: unknown) => request<SalesOrder>(`/api/sales-orders/${id}`, "PUT", body);
export const createSalesOrder = (body: unknown) => post<SalesOrder>("/api/sales-orders", body);
export const deleteSalesOrder = (id: string) => request<void>(`/api/sales-orders/${id}`, "DELETE");
export const cancelSalesOrder = (id: string) => post<SalesOrder>(`/api/sales-orders/${id}/cancel`);
export const cancelKitchenTicket = (id: string) => post<KitchenTicket>(`/api/kitchen/tickets/${id}/cancel`);
export const cancelFiscalDocument = (id: string) => post<FiscalDocument>(`/api/billing/documents/${id}/cancel`);
export const getPrintableReceipt = (id: string) => get<PrintableReceipt>(`/api/billing/documents/${id}/receipt`);

export async function openCashShift(userId: string, openingCash: number) {
  return post<CashShift>("/api/cash-shifts/open", { userId, terminal: "Caja principal", openingCash });
}

export async function closeCashShift(shiftId: string, countedCash: number) {
  return post<CashShift>(`/api/cash-shifts/${shiftId}/close`, { countedCash });
}

export async function advanceKitchenTicket(ticketId: string) {
  return post<KitchenTicket>(`/api/kitchen/tickets/${ticketId}/advance`);
}

export async function releaseTable(number: string) {
  return post<RestaurantTable>(`/api/restaurant/tables/${encodeURIComponent(number)}/release`);
}

export async function loadErpData(permissions: string[]) {
  const can = (permission: string) => permissions.includes(permission);
  const [dashboard, leads, products, orders, customers, users, tables, shifts, kitchenTickets, fiscalDocuments] = await Promise.all([
    get<Dashboard>("/api/dashboard"),
    can(PERMISSIONS.crm) ? get<Lead[]>("/api/crm/leads") : Promise.resolve([]),
    can(PERMISSIONS.productsView) ? get<Product[]>("/api/products") : Promise.resolve([]),
    can(PERMISSIONS.sales) ? get<SalesOrder[]>("/api/sales-orders") : Promise.resolve([]),
    can(PERMISSIONS.customersView) ? get<Customer[]>("/api/customers") : Promise.resolve([]),
    can(PERMISSIONS.users) ? get<UserAccount[]>("/api/users") : Promise.resolve([]),
    can(PERMISSIONS.tablesView) ? get<RestaurantTable[]>("/api/restaurant/tables") : Promise.resolve([]),
    can(PERMISSIONS.cashShifts) ? get<CashShift[]>("/api/cash-shifts") : Promise.resolve([]),
    can(PERMISSIONS.kitchen) ? get<KitchenTicket[]>("/api/kitchen/tickets") : Promise.resolve([]),
    can(PERMISSIONS.billingView) ? get<FiscalDocument[]>("/api/billing/documents") : Promise.resolve([])
  ]);

  return { dashboard, leads, products, orders, customers, users, tables, shifts, kitchenTickets, fiscalDocuments };
}

export const getSettings = () => get<SystemSettings>("/api/settings");
export const getReceiptTemplate = () => get<ReceiptTemplate>("/api/settings/receipt-template");
export const updateSettings = (body: SystemSettings) => request<SystemSettings>("/api/settings", "PUT", body);

export async function getPublicTableMenu(token: string) {
  const response = await fetch(`${API_URL}/api/public/table-ordering/${encodeURIComponent(token)}/menu`);
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { error?: string } | null;
    throw new Error(problem?.error ?? "No se pudo abrir la carta de esta mesa.");
  }
  return response.json() as Promise<PublicTableMenu>;
}

export async function createPublicTableOrder(token: string, body: { guestName?: string; notes?: string; lines: Array<{ productId: string; quantity: number }> }) {
  const response = await fetch(`${API_URL}/api/public/table-ordering/${encodeURIComponent(token)}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { error?: string } | null;
    throw new Error(problem?.error ?? "No se pudo enviar el pedido.");
  }
  return response.json() as Promise<PublicTableOrder>;
}

export async function createPosSale(
  userId: string,
  customerId: string,
  lines: Array<{ productId: string; quantity: number }>,
  serviceType: "DineIn" | "Takeaway" | "Delivery",
  tableNumber: string | undefined,
  paymentMethod: PaymentMethodName,
  documentType: "Receipt" | "Invoice",
  customerTaxId?: string,
  payments?: Array<{ method: PaymentMethodName; amount: number }>
) {
  return post<{ order: SalesOrder; shift: CashShift; kitchenTicket: KitchenTicket; fiscalDocument: FiscalDocument }>("/api/pos/checkout", {
    userId,
    customerId,
    lines,
    notes: "Pedido generado desde el punto de venta gastronomico",
    serviceType,
    tableNumber: serviceType === "DineIn" ? tableNumber : null,
    paymentMethod,
    payments,
    documentType,
    customerTaxId: documentType === "Invoice" ? customerTaxId : null
  });
}
