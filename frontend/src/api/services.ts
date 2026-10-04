import { api } from './client';
import type { Dashboard, Document, Product, StockRow, User, Warehouse } from '../types';
export const authApi = { status: () => api<{ authenticated: boolean; user?: User }>('/api/auth/status'), logout: () => api('/api/auth/logout', { method: 'POST' }) };
export const dashboardApi = { summary: () => api<Dashboard>('/api/dashboard'), alerts: () => api<Array<{ product: string; warehouse: string; quantity: number; safetyStock: number }>>('/api/dashboard/alerts'), distribution: () => api<Array<{ warehouse: string; quantity: number }>>('/api/dashboard/warehouse-distribution'), trend: () => api<Array<{ date: string; inbound: number; outbound: number }>>('/api/dashboard/daily-trend') };
export const masterApi = { products: () => api<Product[]>('/api/products'), warehouses: () => api<Warehouse[]>('/api/warehouses') };
export const inventoryApi = { stock: () => api<StockRow[]>('/api/stock'), transactions: () => api('/api/transactions') };
export const documentsApi = { list: () => api<Document[]>('/api/documents'), create: (body: unknown) => api<Document>('/api/documents', { method: 'POST', body: JSON.stringify(body) }), action: (id: number, action: string) => api<Document>(`/api/documents/${id}/${action}`, { method: 'POST', body: action === 'reverse' ? JSON.stringify({}) : undefined }) };
