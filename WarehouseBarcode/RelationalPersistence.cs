using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WarehouseBarcode;

// The application keeps its existing API model in memory, while this class is the only
// persistence boundary. Every collection is stored in its own relational table.
internal static class RelationalPersistence
{
    static SqliteConnection Open(string path)
    {
        var c = new SqliteConnection($"Data Source={path};Mode=ReadWriteCreate;Cache=Private;Default Timeout=5");
        c.Open(); using var p = c.CreateCommand(); p.CommandText = "PRAGMA foreign_keys=ON;"; p.ExecuteNonQuery(); return c;
    }
    static object V(object? v) => v ?? DBNull.Value;
    static object N(decimal v) => (double)v;
    static void P(SqliteCommand c, string n, object? v) => c.Parameters.AddWithValue(n, V(v));
    static string? S(Dictionary<string, object?> r, string n) => r[n] is null ? null : Convert.ToString(r[n], CultureInfo.InvariantCulture);
    static int I(Dictionary<string, object?> r, string n) => Convert.ToInt32(r[n], CultureInfo.InvariantCulture);
    static long L(Dictionary<string, object?> r, string n) => Convert.ToInt64(r[n], CultureInfo.InvariantCulture);
    static decimal D(Dictionary<string, object?> r, string n) => Convert.ToDecimal(r[n], CultureInfo.InvariantCulture);
    static bool B(Dictionary<string, object?> r, string n) => I(r, n) != 0;
    static int? NI(Dictionary<string, object?> r, string n) => r[n] is null ? null : I(r, n);
    static DateTime? DT(Dictionary<string, object?> r, string n) => DateTime.TryParse(S(r, n), null, DateTimeStyles.RoundtripKind, out var d) ? d : null;
    static DateTime DTR(Dictionary<string, object?> r, string n) => DT(r, n) ?? DateTime.UtcNow;
    static string ISO(DateTime d) => d.ToString("O", CultureInfo.InvariantCulture);
    static string? ISO(DateTime? d) => d?.ToString("O", CultureInfo.InvariantCulture);
    static List<Dictionary<string, object?>> Rows(SqliteConnection c, string sql)
    {
        using var q = c.CreateCommand(); q.CommandText = sql; using var r = q.ExecuteReader(); var result = new List<Dictionary<string, object?>>();
        while (r.Read()) { var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase); for (var i = 0; i < r.FieldCount; i++) row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i); result.Add(row); } return result;
    }
    static void Exec(SqliteTransaction tx, string sql, params (string, object?)[] values)
    {
        using var c = tx.Connection!.CreateCommand(); c.Transaction = tx; c.CommandText = sql; foreach (var (n, v) in values) P(c, n, v); c.ExecuteNonQuery();
    }

    public static DataStore? LoadOrMigrate(string path)
    {
        using var c = Open(path); EnsureSchema(c);
        var hasLegacy = Convert.ToInt32(Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='data_store'")) > 0;
        var legacy = hasLegacy ? Rows(c, "SELECT json FROM data_store WHERE id=1").FirstOrDefault() : null;
        var count = Convert.ToInt32(Scalar(c, "SELECT COUNT(*) FROM warehouses"));
        DataStore? data = null;
        if (count > 0) data = Load(c);
        else if (legacy is not null && legacy["json"] is string json && !string.IsNullOrWhiteSpace(json)) data = JsonSerializer.Deserialize<DataStore>(json);
        return data;
    }
    static object? Scalar(SqliteConnection c, string sql) { using var q = c.CreateCommand(); q.CommandText = sql; return q.ExecuteScalar(); }

    static void EnsureSchema(SqliteConnection c)
    {
        using var q = c.CreateCommand(); q.CommandText = @"
CREATE TABLE IF NOT EXISTS warehouses(id INTEGER PRIMARY KEY,code TEXT NOT NULL UNIQUE,name TEXT NOT NULL,address TEXT,active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS departments(id INTEGER PRIMARY KEY,code TEXT NOT NULL UNIQUE,name TEXT NOT NULL,parent_id INTEGER,manager_username TEXT,active INTEGER NOT NULL DEFAULT 1,FOREIGN KEY(parent_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS roles(code TEXT PRIMARY KEY,name TEXT NOT NULL,active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS permissions(code TEXT PRIMARY KEY,name TEXT NOT NULL,group_name TEXT);
CREATE TABLE IF NOT EXISTS role_permissions(role_code TEXT NOT NULL,permission_code TEXT NOT NULL,PRIMARY KEY(role_code,permission_code),FOREIGN KEY(role_code) REFERENCES roles(code) ON DELETE CASCADE,FOREIGN KEY(permission_code) REFERENCES permissions(code));
CREATE TABLE IF NOT EXISTS users(username TEXT PRIMARY KEY,password TEXT NOT NULL,display_name TEXT NOT NULL,role TEXT NOT NULL,role_code TEXT,department_id INTEGER,phone TEXT,last_login_at TEXT,failed_login_count INTEGER NOT NULL DEFAULT 0,locked_until TEXT,active INTEGER NOT NULL DEFAULT 1,FOREIGN KEY(role_code) REFERENCES roles(code),FOREIGN KEY(department_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS user_roles(username TEXT NOT NULL,role_code TEXT NOT NULL,PRIMARY KEY(username,role_code),FOREIGN KEY(username) REFERENCES users(username) ON DELETE CASCADE,FOREIGN KEY(role_code) REFERENCES roles(code));
CREATE TABLE IF NOT EXISTS user_departments(username TEXT NOT NULL,department_id INTEGER NOT NULL,PRIMARY KEY(username,department_id),FOREIGN KEY(username) REFERENCES users(username) ON DELETE CASCADE,FOREIGN KEY(department_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS business_partners(id INTEGER PRIMARY KEY,code TEXT NOT NULL UNIQUE,name TEXT NOT NULL,type TEXT NOT NULL,contact TEXT,phone TEXT,address TEXT,postal_code TEXT,active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS products(id INTEGER PRIMARY KEY,sku TEXT NOT NULL UNIQUE,barcode TEXT NOT NULL UNIQUE,name TEXT NOT NULL,category TEXT NOT NULL,specification TEXT NOT NULL,unit TEXT NOT NULL,safety_stock REAL NOT NULL,max_stock REAL NOT NULL,batch_managed INTEGER NOT NULL DEFAULT 1,expiry_managed INTEGER NOT NULL DEFAULT 0,issue_strategy TEXT NOT NULL,active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS warehouse_locations(id INTEGER PRIMARY KEY,warehouse_id INTEGER NOT NULL,zone TEXT NOT NULL,shelf TEXT NOT NULL,level TEXT NOT NULL,code TEXT NOT NULL UNIQUE,status TEXT NOT NULL,capacity REAL NOT NULL,occupied REAL NOT NULL,FOREIGN KEY(warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS stock_balances(id INTEGER PRIMARY KEY,product_id INTEGER NOT NULL,warehouse_id INTEGER NOT NULL,location_id INTEGER,batch_no TEXT NOT NULL,produced_at TEXT,expiry_date TEXT,status TEXT NOT NULL,received_at TEXT NOT NULL,quantity REAL NOT NULL,FOREIGN KEY(product_id) REFERENCES products(id),FOREIGN KEY(warehouse_id) REFERENCES warehouses(id),FOREIGN KEY(location_id) REFERENCES warehouse_locations(id));
CREATE TABLE IF NOT EXISTS stock_transactions(id INTEGER PRIMARY KEY,document_no TEXT NOT NULL,source_document_no TEXT,occurred_at TEXT NOT NULL,type TEXT NOT NULL,product_id INTEGER NOT NULL,warehouse_id INTEGER NOT NULL,quantity REAL NOT NULL,remark TEXT,FOREIGN KEY(product_id) REFERENCES products(id),FOREIGN KEY(warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS business_documents(id INTEGER PRIMARY KEY,document_no TEXT NOT NULL UNIQUE,document_type TEXT NOT NULL,document_date TEXT NOT NULL,partner_id INTEGER,warehouse_id INTEGER NOT NULL,status TEXT NOT NULL,remark TEXT,created_by TEXT,auditor TEXT,audited_at TEXT,completed_at TEXT,FOREIGN KEY(partner_id) REFERENCES business_partners(id),FOREIGN KEY(warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS business_document_lines(id INTEGER PRIMARY KEY,document_id INTEGER NOT NULL,product_id INTEGER NOT NULL,barcode TEXT NOT NULL,quantity REAL NOT NULL,location_id INTEGER,batch_no TEXT,produced_at TEXT,expiry_date TEXT,remark TEXT,quality_status TEXT,FOREIGN KEY(document_id) REFERENCES business_documents(id) ON DELETE CASCADE,FOREIGN KEY(product_id) REFERENCES products(id),FOREIGN KEY(location_id) REFERENCES warehouse_locations(id));
CREATE TABLE IF NOT EXISTS transfer_documents(id INTEGER PRIMARY KEY,document_no TEXT NOT NULL UNIQUE,from_warehouse_id INTEGER NOT NULL,to_warehouse_id INTEGER NOT NULL,status TEXT NOT NULL,remark TEXT,created_by TEXT,auditor TEXT,document_date TEXT NOT NULL,completed_at TEXT,FOREIGN KEY(from_warehouse_id) REFERENCES warehouses(id),FOREIGN KEY(to_warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS transfer_lines(id INTEGER PRIMARY KEY,transfer_id INTEGER NOT NULL,product_id INTEGER NOT NULL,quantity REAL NOT NULL,source_location_id INTEGER,target_location_id INTEGER,batch_no TEXT,FOREIGN KEY(transfer_id) REFERENCES transfer_documents(id) ON DELETE CASCADE,FOREIGN KEY(product_id) REFERENCES products(id));
CREATE TABLE IF NOT EXISTS stocktake_documents(id INTEGER PRIMARY KEY,document_no TEXT NOT NULL UNIQUE,warehouse_id INTEGER NOT NULL,status TEXT NOT NULL,remark TEXT,created_by TEXT,auditor TEXT,document_date TEXT NOT NULL,FOREIGN KEY(warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS stocktake_lines(id INTEGER PRIMARY KEY,stocktake_id INTEGER NOT NULL,product_id INTEGER NOT NULL,location_id INTEGER,batch_no TEXT,book_quantity REAL NOT NULL,actual_quantity REAL NOT NULL,FOREIGN KEY(stocktake_id) REFERENCES stocktake_documents(id) ON DELETE CASCADE,FOREIGN KEY(product_id) REFERENCES products(id));
CREATE TABLE IF NOT EXISTS stocktakes(id INTEGER PRIMARY KEY,occurred_at TEXT NOT NULL,product_id INTEGER NOT NULL,warehouse_id INTEGER NOT NULL,before_quantity REAL NOT NULL,after_quantity REAL NOT NULL,difference REAL NOT NULL,remark TEXT,FOREIGN KEY(product_id) REFERENCES products(id),FOREIGN KEY(warehouse_id) REFERENCES warehouses(id));
CREATE TABLE IF NOT EXISTS audit_logs(id INTEGER PRIMARY KEY,occurred_at TEXT NOT NULL,username TEXT,action TEXT NOT NULL,entity_type TEXT,entity_id TEXT,detail TEXT);
CREATE TABLE IF NOT EXISTS schema_versions(version INTEGER PRIMARY KEY,applied_at TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_stock_product_warehouse ON stock_balances(product_id,warehouse_id);
CREATE INDEX IF NOT EXISTS idx_transactions_occurred ON stock_transactions(occurred_at);"; q.ExecuteNonQuery();
    }

    static DataStore Load(SqliteConnection c)
    {
        var d = new DataStore();
        foreach (var r in Rows(c,"SELECT * FROM warehouses ORDER BY id")) d.Warehouses.Add(new Warehouse{Id=I(r,"id"),Code=S(r,"code")??"",Name=S(r,"name")??"",Address=S(r,"address"),Active=B(r,"active")});
        foreach (var r in Rows(c,"SELECT * FROM departments ORDER BY id")) d.Departments.Add(new Department{Id=I(r,"id"),Code=S(r,"code")??"",Name=S(r,"name")??"",ParentId=NI(r,"parent_id"),ManagerUsername=S(r,"manager_username"),Active=B(r,"active")});
        var roles=d.Roles.ToDictionary(x=>x.Code); foreach(var r in Rows(c,"SELECT * FROM roles ORDER BY code")){var x=new RoleDefinition{Code=S(r,"code")??"",Name=S(r,"name")??"",Active=B(r,"active")};d.Roles.Add(x);roles[x.Code]=x;} foreach(var r in Rows(c,"SELECT * FROM role_permissions")){if(roles.TryGetValue(S(r,"role_code")??"",out var x))x.Permissions.Add(S(r,"permission_code")??"");}
        foreach(var r in Rows(c,"SELECT * FROM business_partners ORDER BY id")) d.Partners.Add(new BusinessPartner{Id=I(r,"id"),Code=S(r,"code")??"",Name=S(r,"name")??"",Type=S(r,"type")??"",Contact=S(r,"contact"),Phone=S(r,"phone"),Address=S(r,"address"),PostalCode=S(r,"postal_code"),Active=B(r,"active")});
        foreach(var r in Rows(c,"SELECT * FROM products ORDER BY id")) d.Products.Add(new Product{Id=I(r,"id"),Sku=S(r,"sku")??"",Barcode=S(r,"barcode")??"",Name=S(r,"name")??"",Category=S(r,"category")??"",Specification=S(r,"specification")??"",Unit=S(r,"unit")??"",SafetyStock=D(r,"safety_stock"),MaxStock=D(r,"max_stock"),BatchManaged=B(r,"batch_managed"),ExpiryManaged=B(r,"expiry_managed"),IssueStrategy=S(r,"issue_strategy")??"FIFO",Active=B(r,"active")});
        foreach(var r in Rows(c,"SELECT * FROM warehouse_locations ORDER BY id")) d.Locations.Add(new WarehouseLocation{Id=I(r,"id"),WarehouseId=I(r,"warehouse_id"),Zone=S(r,"zone")??"",Shelf=S(r,"shelf")??"",Level=S(r,"level")??"",Code=S(r,"code")??"",Status=S(r,"status")??"",Capacity=D(r,"capacity"),Occupied=D(r,"occupied")});
        foreach(var r in Rows(c,"SELECT * FROM stock_balances ORDER BY id")) d.Stock.Add(new StockBalance{Id=I(r,"id"),ProductId=I(r,"product_id"),WarehouseId=I(r,"warehouse_id"),LocationId=NI(r,"location_id"),BatchNo=S(r,"batch_no")??"",ProducedAt=DT(r,"produced_at"),ExpiryDate=DT(r,"expiry_date"),Status=S(r,"status")??"正常",ReceivedAt=DTR(r,"received_at"),Quantity=D(r,"quantity")});
        foreach(var r in Rows(c,"SELECT * FROM stock_transactions ORDER BY id")) d.Transactions.Add(new StockTransaction{Id=L(r,"id"),DocumentNo=S(r,"document_no")??"",SourceDocumentNo=S(r,"source_document_no"),OccurredAt=DTR(r,"occurred_at"),Type=S(r,"type")??"",ProductId=I(r,"product_id"),WarehouseId=I(r,"warehouse_id"),Quantity=D(r,"quantity"),Remark=S(r,"remark")});
        foreach(var r in Rows(c,"SELECT * FROM business_documents ORDER BY id")) d.Documents.Add(new BusinessDocument{Id=L(r,"id"),DocumentNo=S(r,"document_no")??"",DocumentType=S(r,"document_type")??"",DocumentDate=DTR(r,"document_date"),PartnerId=NI(r,"partner_id"),WarehouseId=I(r,"warehouse_id"),Status=S(r,"status")??"",Remark=S(r,"remark"),CreatedBy=S(r,"created_by"),Auditor=S(r,"auditor"),AuditedAt=DT(r,"audited_at"),CompletedAt=DT(r,"completed_at")});
        var docs=d.Documents.ToDictionary(x=>x.Id); foreach(var r in Rows(c,"SELECT * FROM business_document_lines ORDER BY id")){if(docs.TryGetValue(L(r,"document_id"),out var x))x.Lines.Add(new BusinessDocumentLine{Id=L(r,"id"),DocumentId=L(r,"document_id"),ProductId=I(r,"product_id"),Barcode=S(r,"barcode")??"",Quantity=D(r,"quantity"),LocationId=NI(r,"location_id"),BatchNo=S(r,"batch_no"),ProducedAt=DT(r,"produced_at"),ExpiryDate=DT(r,"expiry_date"),Remark=S(r,"remark"),QualityStatus=S(r,"quality_status")});}
        foreach(var r in Rows(c,"SELECT * FROM transfer_documents ORDER BY id")) d.TransferDocuments.Add(new TransferDocument{Id=L(r,"id"),DocumentNo=S(r,"document_no")??"",FromWarehouseId=I(r,"from_warehouse_id"),ToWarehouseId=I(r,"to_warehouse_id"),Status=S(r,"status")??"",Remark=S(r,"remark"),CreatedBy=S(r,"created_by"),Auditor=S(r,"auditor"),DocumentDate=DTR(r,"document_date"),CompletedAt=DT(r,"completed_at")}); var transfers=d.TransferDocuments.ToDictionary(x=>x.Id); foreach(var r in Rows(c,"SELECT * FROM transfer_lines ORDER BY id")){if(transfers.TryGetValue(L(r,"transfer_id"),out var x))x.Lines.Add(new TransferLine{Id=L(r,"id"),ProductId=I(r,"product_id"),Quantity=D(r,"quantity"),SourceLocationId=NI(r,"source_location_id"),TargetLocationId=NI(r,"target_location_id"),BatchNo=S(r,"batch_no")});}
        foreach(var r in Rows(c,"SELECT * FROM stocktake_documents ORDER BY id")) d.StocktakeDocuments.Add(new StocktakeDocument{Id=L(r,"id"),DocumentNo=S(r,"document_no")??"",WarehouseId=I(r,"warehouse_id"),Status=S(r,"status")??"",Remark=S(r,"remark"),CreatedBy=S(r,"created_by"),Auditor=S(r,"auditor"),DocumentDate=DTR(r,"document_date")}); var takes=d.StocktakeDocuments.ToDictionary(x=>x.Id); foreach(var r in Rows(c,"SELECT * FROM stocktake_lines ORDER BY id")){if(takes.TryGetValue(L(r,"stocktake_id"),out var x))x.Lines.Add(new StocktakeLine{Id=L(r,"id"),ProductId=I(r,"product_id"),LocationId=NI(r,"location_id"),BatchNo=S(r,"batch_no"),BookQuantity=D(r,"book_quantity"),ActualQuantity=D(r,"actual_quantity")});}
        foreach(var r in Rows(c,"SELECT * FROM stocktakes ORDER BY id")) d.Stocktakes.Add(new Stocktake{Id=L(r,"id"),OccurredAt=DTR(r,"occurred_at"),ProductId=I(r,"product_id"),WarehouseId=I(r,"warehouse_id"),Before=D(r,"before_quantity"),After=D(r,"after_quantity"),Difference=D(r,"difference"),Remark=S(r,"remark")});
        foreach(var r in Rows(c,"SELECT * FROM users ORDER BY username")) d.Users.Add(new UserAccount{Username=S(r,"username")??"",Password=S(r,"password")??"",DisplayName=S(r,"display_name")??"",Role=S(r,"role")??"",RoleCode=S(r,"role_code"),DepartmentId=NI(r,"department_id"),Phone=S(r,"phone"),LastLoginAt=DT(r,"last_login_at"),FailedLoginCount=I(r,"failed_login_count"),LockedUntil=DT(r,"locked_until"),Active=B(r,"active")});
        foreach(var r in Rows(c,"SELECT * FROM audit_logs ORDER BY id")) d.AuditLogs.Add(new AuditLog{Id=L(r,"id"),OccurredAt=DTR(r,"occurred_at"),Username=S(r,"username"),Action=S(r,"action")??"",EntityType=S(r,"entity_type"),EntityId=S(r,"entity_id"),Detail=S(r,"detail")}); return d;
    }

    public static void Save(string path, DataStore d)
    {
        using var c=Open(path); using var tx=c.BeginTransaction(); Exec(tx,"PRAGMA defer_foreign_keys=ON");
        foreach(var t in new[]{"role_permissions","user_roles","user_departments","business_document_lines","transfer_lines","stocktake_lines","stock_transactions","stock_balances","business_documents","transfer_documents","stocktake_documents","stocktakes","users","warehouse_locations","products","business_partners","departments","roles","permissions","warehouses","audit_logs"}) Exec(tx,$"DELETE FROM {t}");
        foreach(var x in d.Warehouses)Exec(tx,"INSERT INTO warehouses VALUES($id,$code,$name,$address,$active)",("$id",x.Id),("$code",x.Code),("$name",x.Name),("$address",x.Address),("$active",x.Active?1:0));
        foreach(var x in d.Departments)Exec(tx,"INSERT INTO departments VALUES($id,$code,$name,$parent,$manager,$active)",("$id",x.Id),("$code",x.Code),("$name",x.Name),("$parent",x.ParentId),("$manager",x.ManagerUsername),("$active",x.Active?1:0));
        foreach(var x in d.Roles)Exec(tx,"INSERT INTO roles VALUES($code,$name,$active)",("$code",x.Code),("$name",x.Name),("$active",x.Active?1:0)); foreach(var p in d.Roles.SelectMany(x=>x.Permissions).Distinct())Exec(tx,"INSERT INTO permissions VALUES($code,$name,$group)",("$code",p),("$name",p),("$group",p.Split('.')[0])); foreach(var x in d.Roles.SelectMany(r=>r.Permissions.Select(p=>(r.Code,p))))Exec(tx,"INSERT INTO role_permissions VALUES($role,$permission)",("$role",x.Code),("$permission",x.p));
        foreach(var x in d.Users){Exec(tx,"INSERT INTO users VALUES($username,$password,$display,$role,$role_code,$department,$phone,$last,$failed,$locked,$active)",("$username",x.Username),("$password",x.Password),("$display",x.DisplayName),("$role",x.Role),("$role_code",x.RoleCode),("$department",x.DepartmentId),("$phone",x.Phone),("$last",ISO(x.LastLoginAt)),("$failed",x.FailedLoginCount),("$locked",ISO(x.LockedUntil)),("$active",x.Active?1:0));if(x.RoleCode is not null)Exec(tx,"INSERT INTO user_roles VALUES($username,$role)",("$username",x.Username),("$role",x.RoleCode));if(x.DepartmentId is not null)Exec(tx,"INSERT INTO user_departments VALUES($username,$department)",("$username",x.Username),("$department",x.DepartmentId));}
        foreach(var x in d.Partners)Exec(tx,"INSERT INTO business_partners VALUES($id,$code,$name,$type,$contact,$phone,$address,$postal,$active)",("$id",x.Id),("$code",x.Code),("$name",x.Name),("$type",x.Type),("$contact",x.Contact),("$phone",x.Phone),("$address",x.Address),("$postal",x.PostalCode),("$active",x.Active?1:0));
        foreach(var x in d.Products)Exec(tx,"INSERT INTO products VALUES($id,$sku,$barcode,$name,$category,$spec,$unit,$safety,$max,$batch,$expiry,$strategy,$active)",("$id",x.Id),("$sku",x.Sku),("$barcode",x.Barcode),("$name",x.Name),("$category",x.Category),("$spec",x.Specification),("$unit",x.Unit),("$safety",N(x.SafetyStock)),("$max",N(x.MaxStock)),("$batch",x.BatchManaged?1:0),("$expiry",x.ExpiryManaged?1:0),("$strategy",x.IssueStrategy),("$active",x.Active?1:0));
        foreach(var x in d.Locations)Exec(tx,"INSERT INTO warehouse_locations VALUES($id,$warehouse,$zone,$shelf,$level,$code,$status,$capacity,$occupied)",("$id",x.Id),("$warehouse",x.WarehouseId),("$zone",x.Zone),("$shelf",x.Shelf),("$level",x.Level),("$code",x.Code),("$status",x.Status),("$capacity",N(x.Capacity)),("$occupied",N(x.Occupied)));
        foreach(var x in d.Stock)Exec(tx,"INSERT INTO stock_balances VALUES($id,$product,$warehouse,$location,$batch,$produced,$expiry,$status,$received,$quantity)",("$id",x.Id),("$product",x.ProductId),("$warehouse",x.WarehouseId),("$location",x.LocationId),("$batch",x.BatchNo),("$produced",ISO(x.ProducedAt)),("$expiry",ISO(x.ExpiryDate)),("$status",x.Status),("$received",ISO(x.ReceivedAt)),("$quantity",N(x.Quantity)));
        foreach(var x in d.Transactions)Exec(tx,"INSERT INTO stock_transactions VALUES($id,$document,$source,$occurred,$type,$product,$warehouse,$quantity,$remark)",("$id",x.Id),("$document",x.DocumentNo),("$source",x.SourceDocumentNo),("$occurred",ISO(x.OccurredAt)),("$type",x.Type),("$product",x.ProductId),("$warehouse",x.WarehouseId),("$quantity",N(x.Quantity)),("$remark",x.Remark));
        long documentLineId=1; foreach(var x in d.Documents){Exec(tx,"INSERT INTO business_documents VALUES($id,$no,$type,$date,$partner,$warehouse,$status,$remark,$created,$auditor,$audited,$completed)",("$id",x.Id),("$no",x.DocumentNo),("$type",x.DocumentType),("$date",ISO(x.DocumentDate)),("$partner",x.PartnerId),("$warehouse",x.WarehouseId),("$status",x.Status),("$remark",x.Remark),("$created",x.CreatedBy),("$auditor",x.Auditor),("$audited",ISO(x.AuditedAt)),("$completed",ISO(x.CompletedAt)));foreach(var l in x.Lines)Exec(tx,"INSERT INTO business_document_lines VALUES($id,$document,$product,$barcode,$quantity,$location,$batch,$produced,$expiry,$remark,$quality)",("$id",documentLineId++),("$document",x.Id),("$product",l.ProductId),("$barcode",l.Barcode),("$quantity",N(l.Quantity)),("$location",l.LocationId),("$batch",l.BatchNo),("$produced",ISO(l.ProducedAt)),("$expiry",ISO(l.ExpiryDate)),("$remark",l.Remark),("$quality",l.QualityStatus));}
        long transferLineId=1; foreach(var x in d.TransferDocuments){Exec(tx,"INSERT INTO transfer_documents VALUES($id,$no,$from,$to,$status,$remark,$created,$auditor,$date,$completed)",("$id",x.Id),("$no",x.DocumentNo),("$from",x.FromWarehouseId),("$to",x.ToWarehouseId),("$status",x.Status),("$remark",x.Remark),("$created",x.CreatedBy),("$auditor",x.Auditor),("$date",ISO(x.DocumentDate)),("$completed",ISO(x.CompletedAt)));foreach(var l in x.Lines)Exec(tx,"INSERT INTO transfer_lines VALUES($id,$transfer,$product,$quantity,$source,$target,$batch)",("$id",transferLineId++),("$transfer",x.Id),("$product",l.ProductId),("$quantity",N(l.Quantity)),("$source",l.SourceLocationId),("$target",l.TargetLocationId),("$batch",l.BatchNo));}
        long stocktakeLineId=1; foreach(var x in d.StocktakeDocuments){Exec(tx,"INSERT INTO stocktake_documents VALUES($id,$no,$warehouse,$status,$remark,$created,$auditor,$date)",("$id",x.Id),("$no",x.DocumentNo),("$warehouse",x.WarehouseId),("$status",x.Status),("$remark",x.Remark),("$created",x.CreatedBy),("$auditor",x.Auditor),("$date",ISO(x.DocumentDate)));foreach(var l in x.Lines)Exec(tx,"INSERT INTO stocktake_lines VALUES($id,$take,$product,$location,$batch,$book,$actual)",("$id",stocktakeLineId++),("$take",x.Id),("$product",l.ProductId),("$location",l.LocationId),("$batch",l.BatchNo),("$book",N(l.BookQuantity)),("$actual",N(l.ActualQuantity)));}
        foreach(var x in d.Stocktakes)Exec(tx,"INSERT INTO stocktakes VALUES($id,$occurred,$product,$warehouse,$before,$after,$difference,$remark)",("$id",x.Id),("$occurred",ISO(x.OccurredAt)),("$product",x.ProductId),("$warehouse",x.WarehouseId),("$before",N(x.Before)),("$after",N(x.After)),("$difference",N(x.Difference)),("$remark",x.Remark)); foreach(var x in d.AuditLogs)Exec(tx,"INSERT INTO audit_logs VALUES($id,$occurred,$username,$action,$entity_type,$entity_id,$detail)",("$id",x.Id),("$occurred",ISO(x.OccurredAt)),("$username",x.Username),("$action",x.Action),("$entity_type",x.EntityType),("$entity_id",x.EntityId),("$detail",x.Detail));
        Exec(tx,"INSERT OR REPLACE INTO schema_versions VALUES(2,$at)",("$at",ISO(DateTime.UtcNow))); Exec(tx,"DROP TABLE IF EXISTS data_store"); tx.Commit();
    }
    public static void Backup(string source, string destination) { using var a=Open(source); using var b=Open(destination); a.BackupDatabase(b); }
}
