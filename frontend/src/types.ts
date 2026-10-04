export type User = { username: string; role: string; displayName?: string };
export type Product = { id: number; sku: string; barcode: string; name: string; unit: string; safetyStock: number };
export type Warehouse = { id: number; code: string; name: string };
export type StockRow = { id: number; sku: string; barcode: string; product: string; warehouse: string; location?: string; batchNo: string; producedAt?: string; expiryDate?: string; status: string; quantity: number; unit: string; safetyStock: number };
export type Dashboard = { products: number; warehouses: number; partners: number; lowStock: number; recent: Array<{ documentNo: string; type: string; quantity: number; product: string; warehouse: string; occurredAt: string }> };
export type Document = { id: number; documentNo: string; documentType: string; documentDate: string; warehouseId: number; partnerId?: number; status: string; remark?: string; lines: DocumentLine[] };
export type DocumentLine = { productId: number; quantity: number; batchNo?: string; expiryDate?: string; locationId?: number };
