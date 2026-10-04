namespace WarehouseBarcode;

public sealed class WarehouseLocation
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Zone { get; set; } = "";
    public string Shelf { get; set; } = "";
    public string Level { get; set; } = "";
    public string Code { get; set; } = "";
    public string Status { get; set; } = "启用";
    public decimal Capacity { get; set; }
    public decimal Occupied { get; set; }
}

public sealed class BatchStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public string BatchNo { get; set; } = "LEGACY-IMPORT";
    public DateTime? ProducedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "正常";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public decimal Quantity { get; set; }
}

public sealed class TransferDocument
{
    public long Id { get; set; }
    public string DocumentNo { get; set; } = "";
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public string Status { get; set; } = "草稿";
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public string? Auditor { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<TransferLine> Lines { get; set; } = [];
}

public sealed class TransferLine
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public int? SourceLocationId { get; set; }
    public int? TargetLocationId { get; set; }
    public string? BatchNo { get; set; }
}

public sealed class StocktakeDocument
{
    public long Id { get; set; }
    public string DocumentNo { get; set; } = "";
    public int WarehouseId { get; set; }
    public string Status { get; set; } = "草稿";
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public string? Auditor { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
    public List<StocktakeLine> Lines { get; set; } = [];
}

public sealed class StocktakeLine
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public int? LocationId { get; set; }
    public string? BatchNo { get; set; }
    public decimal BookQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal Difference => ActualQuantity - BookQuantity;
}

