import json
from pathlib import Path
p=Path('WarehouseBarcode/warehouse-data.json')
d=json.loads(p.read_text())
d['Users'][0]['Password']='QoUKLIzJr+YnE12ZGjChsw==.Vg8HG75+95DG8Iak5KA/OPRK96QwKfMZjztzWkeTXl0='
# The dataset is fictional. Reconcile every stock balance against its signed ledger.
d['Transactions']=[]
d['Documents']=[]
products={x['Id']:x for x in d['Products']}
def document(stock, qty, kind, date, status='已完成', remark='采购到货，验收合格'):
    i=len(d['Documents'])+1
    no=f"{'IN' if kind=='入库单' else 'OUT'}-{date[:10].replace('-','')}-{i:04d}"
    line=dict(Id=1,DocumentId=i,ProductId=stock['ProductId'],Barcode=products[stock['ProductId']]['Barcode'],Quantity=qty,LocationId=stock['LocationId'],BatchNo=stock['BatchNo'],ProducedAt=stock['ProducedAt'],ExpiryDate=stock['ExpiryDate'],QualityStatus='合格',Remark=remark)
    doc=dict(Id=i,DocumentNo=no,DocumentType=kind,DocumentDate=date,PartnerId=20 if kind=='入库单' else 22,WarehouseId=stock['WarehouseId'],Status=status,Remark=remark,CreatedBy='operator',Auditor='admin' if status=='已完成' else None,AuditedAt=date if status=='已完成' else None,CompletedAt=date if status=='已完成' else None,Lines=[line])
    d['Documents'].append(doc)
    if status=='已完成':
        d['Transactions'].append(dict(Id=len(d['Transactions'])+1,DocumentNo=no+'-1',SourceDocumentNo=no,OccurredAt=date,Type='入库' if kind=='入库单' else '出库',ProductId=stock['ProductId'],WarehouseId=stock['WarehouseId'],Quantity=qty,Remark=remark))
# Initial receipt and seven days of realistic picking; quantities reconcile to remaining stock.
for s in d['Stock']:
    outs=[(day, (s['Id']%5+1)*(day%3+1)) for day in range(28,35)]
    document(s,s['Quantity']+sum(q for _,q in outs)+(4 if s['ProductId']==1003 else 0),'入库单',s['ReceivedAt'])
    for day,q in outs:
        date=f'2026-09-{day:02d}' if day<=30 else f'2026-10-{day-30:02d}'
        document(s,q,'出库单',date+'T08:30:00Z',remark='客户订单配货' if s['ProductId'] in (1000,1005) else '生产与维修领用')
s=d['Stock'][5]
d['Stocktakes']=[dict(Id=1,OccurredAt='2026-10-04T09:00:00Z',ProductId=s['ProductId'],WarehouseId=1,Before=68,After=64,Difference=-4,Remark='循环盘点，破损4包转损耗')]
d['Transactions'].append(dict(Id=len(d['Transactions'])+1,DocumentNo='ST-20261004-0001',SourceDocumentNo='ST-20261004-0001',OccurredAt='2026-10-04T09:00:00Z',Type='盘点调整',ProductId=s['ProductId'],WarehouseId=1,Quantity=4,Remark='账面:68 实盘:64；破损转损耗'))
d['StocktakeDocuments']=[dict(Id=1,DocumentNo='ST-20261004-0001',WarehouseId=1,Status='已完成',Remark='五金区循环盘点',CreatedBy='operator',Auditor='admin',DocumentDate='2026-10-04T09:00:00Z',Lines=[dict(Id=1,ProductId=s['ProductId'],LocationId=s['LocationId'],BatchNo=s['BatchNo'],BookQuantity=68,ActualQuantity=64)])]
d['TransferDocuments']=[dict(Id=1,DocumentNo='TR-20261004-0001',FromWarehouseId=1,ToWarehouseId=2,Status='已提交',Remark='华南仓节后补货，待管理员审核',CreatedBy='operator',Auditor=None,DocumentDate='2026-10-04T09:30:00Z',CompletedAt=None,Lines=[dict(Id=1,ProductId=1000,Quantity=120,SourceLocationId=10,TargetLocationId=13,BatchNo=d['Stock'][0]['BatchNo'])])]
document(d['Stock'][2],80,'入库单','2026-10-04T10:00:00Z','草稿','气泡膜低库存补货计划')
document(d['Stock'][0],60,'出库单','2026-10-04T10:30:00Z','已提交','待审核：客户追加包装订单')
# Keep occupancy internally consistent in this simplified quantity-based demo.
for l in d['Locations']:
    l['Occupied']=sum(s['Quantity'] for s in d['Stock'] if s['LocationId']==l['Id'])
    l['Capacity']=max(l['Capacity'], ((l['Occupied']//100)+3)*100)
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
# Export a reusable sample, without login secrets.
sample=dict(d); sample['Users']=[]
Path('demo-data').mkdir(exist_ok=True)
Path('demo-data/warehouse-demo.json').write_text(json.dumps(sample,ensure_ascii=False,indent=2)+'\n')
print({k:len(v) for k,v in d.items()})
for s in d['Stock']:
    total=sum(t['Quantity']*(-1 if t['Type'] in ('出库','盘点调整') else 1) for t in d['Transactions'] if t['ProductId']==s['ProductId'] and t['WarehouseId']==s['WarehouseId'])
    assert total==s['Quantity'],(s['Id'],total,s['Quantity'])
print('All stock balances reconcile with transactions.')
