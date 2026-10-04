(() => {
  const $ = (selector) => document.querySelector(selector);
  const request = async (url, options) => { const response = await fetch(url, options); if (!response.ok) throw new Error(await response.text()); return response.status === 204 ? null : response.json(); };
  let products = [];
  let lines = [];

  function renderLines() { $('#document-lines').innerHTML = lines.map((line) => { const product = products.find((item) => item.id === line.productId); return `<tr><td>${product?.name ?? ''}</td><td>${line.quantity}</td><td>${line.batchNo ?? ''}</td><td>${line.expiryDate ?? ''}</td></tr>`; }).join(''); }
  async function loadDocuments() { const documents = await request('/api/documents'); $('#documents').innerHTML = documents.map((doc) => `<tr><td>${doc.documentNo}</td><td>${doc.documentType}</td><td class="status">${doc.status}</td><td>${doc.status === '草稿' ? `<button data-action="submit" data-id="${doc.id}">提交</button>` : ''}${doc.status === '已提交' ? `<button data-action="approve" data-id="${doc.id}">审核</button>` : ''}<button class="print-button" data-action="print" data-id="${doc.id}">打印</button></td></tr>`).join(''); }
  $('#add-line').addEventListener('click', () => { lines.push({ productId: Number($('#product').value), quantity: Number($('#quantity').value), batchNo: $('#batch').value || null, expiryDate: $('#expiry').value || null }); renderLines(); });
  $('#create-document').addEventListener('click', async () => { if (!lines.length) return alert('请添加明细'); const document = await request('/api/documents', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ documentType: $('#document-type').value, warehouseId: Number($('#warehouse').value), lines }) }); lines = []; renderLines(); await loadDocuments(); alert(`已创建 ${document.documentNo}`); });
  $('#documents').addEventListener('click', async (event) => { const button = event.target.closest('button'); if (!button) return; const id = button.dataset.id; if (button.dataset.action === 'print') window.open(`/print/document/${id}`, '_blank'); else { await request(`/api/documents/${id}/${button.dataset.action}`, { method: 'POST' }); await loadDocuments(); } });
  async function init() { products = await request('/api/products'); const warehouses = await request('/api/warehouses'); $('#product').innerHTML = products.map((item) => `<option value="${item.id}">${item.sku} · ${item.name}</option>`).join(''); $('#warehouse').innerHTML = warehouses.map((item) => `<option value="${item.id}">${item.name}</option>`).join(''); loadDocuments(); }
  $('#reload-documents').addEventListener('click', loadDocuments);
  init();
})();
