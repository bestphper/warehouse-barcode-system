export async function api<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, { credentials: 'include', ...options, headers: { 'Content-Type': 'application/json', ...(options?.headers ?? {}) } });
  if (response.status === 401) { window.location.href = '/'; throw new Error('登录已过期'); }
  if (!response.ok) throw new Error((await response.text()) || `请求失败：${response.status}`);
  return response.status === 204 ? (undefined as T) : response.json();
}
