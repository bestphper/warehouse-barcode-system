import { api } from './client';
import type { Dashboard, Document, Product, StockRow, User, Warehouse } from '../types';
type RawAuthStatus = { authenticated: boolean; user?: User; Username?: string; username?: string; Role?: string; role?: string; DisplayName?: string; displayName?: string };
export const authApi = {
  status: async () => {
    const raw = await api<RawAuthStatus>('/api/auth/status');
    if (!raw.authenticated) return { authenticated: false };
    const source = raw.user;
    return { authenticated: true, user: { username: source?.username ?? raw.username ?? raw.Username ?? '', role: source?.role ?? raw.role ?? raw.Role ?? '', displayName: source?.displayName ?? raw.displayName ?? raw.DisplayName } };
  },
  logout: () => api('/api/auth/logout', { method: 'POST' })
};
export const dashboardApi = { summary: () => api<Dashboard>('/api/dashboard'), alerts: () => api<Array<{ product: string; warehouse: string; quantity: number; safetyStock: number }>>('/api/dashboard/alerts'), distribution: () => api<Array<{ warehouse: string; quantity: number }>>('/api/dashboard/warehouse-distribution'), trend: () => api<Array<{ date: string; inbound: number; outbound: number }>>('/api/dashboard/daily-trend') };
export const masterApi = { products: () => api<Product[]>('/api/products'), warehouses: () => api<Warehouse[]>('/api/warehouses') };
export const inventoryApi = { stock: () => api<StockRow[]>('/api/stock'), transactions: () => api('/api/transactions') };
export const documentsApi = { list: () => api<Document[]>('/api/documents'), create: (body: unknown) => api<Document>('/api/documents', { method: 'POST', body: JSON.stringify(body) }), action: (id: number, action: string) => api<Document>(`/api/documents/${id}/${action}`, { method: 'POST', body: action === 'reverse' ? JSON.stringify({}) : undefined }) };
export const transferApi = { list: () => api<import('../types').TransferDocument[]>('/api/transfer-documents'), create: (body: unknown) => api('/api/transfer-documents', { method: 'POST', body: JSON.stringify(body) }), action: (id: number, action: string) => api(`/api/transfer-documents/${id}/${action}`, { method: 'POST' }) };
export const stocktakeApi = { list: () => api<import('../types').StocktakeDocument[]>('/api/stocktake-documents'), create: (body: unknown) => api('/api/stocktake-documents', { method: 'POST', body: JSON.stringify(body) }), action: (id: number, action: string) => api(`/api/stocktake-documents/${id}/${action}`, { method: 'POST' }), exportCsv: (id: number) => `/api/stocktake-documents/${id}/export` };
export const reportApi = { expiry: () => api<Array<{ id: number; product?: string; warehouse?: string; batchNo: string; expiryDate?: string; quantity: number; days?: number; level: string }>>('/api/reports/expiry') };
