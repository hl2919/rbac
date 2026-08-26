using SqlSugar;

namespace RbacWebApi.Models.Wms;

// ============================================================
//  业务单据：收货 → 入库 → 上架 → 下架 → 拣货 → 出库
//  每个单据由 主表 + 明细表 组成；主表用 BaseEntity
//  明细表通过 MasterId 关联主表，独立 Ulid 主键 + CreateTime
// ============================================================

/// <summary>单据明细基类：所有明细表统一字段</summary>
public abstract class WmsDocumentDetailBase : BaseEntity
{
    /// <summary>主表 Id</summary>
    [SugarColumn(ColumnName = "master_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string MasterId { get; set; } = string.Empty;

    /// <summary>行号（从 1 开始）</summary>
    [SugarColumn(ColumnName = "line_no", IsNullable = false)]
    public int LineNo { get; set; }

    /// <summary>货品 Id</summary>
    [SugarColumn(ColumnName = "product_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>批次号</summary>
    [SugarColumn(ColumnName = "batch_no", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? BatchNo { get; set; }

    /// <summary>备注</summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }
}

// ============================================================
//  1. 收货单：供应商到货登记
// ============================================================
[SugarTable("wms_receive_order")]
public class ReceiveOrder : BaseEntity
{
    [SugarColumn(ColumnName = "receive_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ReceiveNo { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "warehouse_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string WarehouseId { get; set; } = string.Empty;

    /// <summary>供应商</summary>
    [SugarColumn(ColumnName = "supplier", Length = 200, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Supplier { get; set; }

    /// <summary>预计到货时间</summary>
    [SugarColumn(ColumnName = "expected_arrival_time", IsNullable = true)]
    public DateTimeOffset? ExpectedArrivalTime { get; set; }

    /// <summary>实际到货时间</summary>
    [SugarColumn(ColumnName = "actual_arrival_time", IsNullable = true)]
    public DateTimeOffset? ActualArrivalTime { get; set; }

    /// <summary>总数量</summary>
    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>总金额</summary>
    [SugarColumn(ColumnName = "total_amount", IsNullable = true)]
    public decimal? TotalAmount { get; set; }

    /// <summary>状态：0=待收货, 1=收货中, 2=已收货, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    /// <summary>操作员</summary>
    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<ReceiveOrderDetail> Details { get; set; } = [];
}

/// <summary>收货单明细</summary>
[SugarTable("wms_receive_order_detail")]
public class ReceiveOrderDetail : WmsDocumentDetailBase
{
    /// <summary>预计数量</summary>
    [SugarColumn(ColumnName = "expected_qty", IsNullable = false)]
    public decimal ExpectedQty { get; set; }

    /// <summary>实收数量</summary>
    [SugarColumn(ColumnName = "received_qty", IsNullable = false)]
    public decimal ReceivedQty { get; set; }

    /// <summary>单价</summary>
    [SugarColumn(ColumnName = "unit_price", IsNullable = true)]
    public decimal? UnitPrice { get; set; }
}

// ============================================================
//  2. 入库单：收货后登记入库
// ============================================================
[SugarTable("wms_inbound_order")]
public class InboundOrder : BaseEntity
{
    [SugarColumn(ColumnName = "inbound_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string InboundNo { get; set; } = string.Empty;

    /// <summary>关联收货单 Id</summary>
    [SugarColumn(ColumnName = "receive_order_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? ReceiveOrderId { get; set; }

    [SugarColumn(ColumnName = "warehouse_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string WarehouseId { get; set; } = string.Empty;

    /// <summary>总数量</summary>
    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>状态：0=待入库, 1=入库中, 2=已入库, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<InboundOrderDetail> Details { get; set; } = [];
}

[SugarTable("wms_inbound_order_detail")]
public class InboundOrderDetail : WmsDocumentDetailBase
{
    /// <summary>入库数量</summary>
    [SugarColumn(ColumnName = "inbound_qty", IsNullable = false)]
    public decimal InboundQty { get; set; }

    /// <summary>目标库位 Id</summary>
    [SugarColumn(ColumnName = "location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? LocationId { get; set; }
}

// ============================================================
//  3. 上架单：把入库的货品放到货架上
// ============================================================
[SugarTable("wms_putaway_order")]
public class PutawayOrder : BaseEntity
{
    [SugarColumn(ColumnName = "putaway_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string PutawayNo { get; set; } = string.Empty;

    /// <summary>关联入库单 Id</summary>
    [SugarColumn(ColumnName = "inbound_order_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? InboundOrderId { get; set; }

    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>状态：0=待上架, 1=上架中, 2=已上架, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<PutawayOrderDetail> Details { get; set; } = [];
}

[SugarTable("wms_putaway_order_detail")]
public class PutawayOrderDetail : WmsDocumentDetailBase
{
    /// <summary>上架数量</summary>
    [SugarColumn(ColumnName = "qty", IsNullable = false)]
    public decimal Qty { get; set; }

    /// <summary>来源库位（暂存区库位）</summary>
    [SugarColumn(ColumnName = "source_location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? SourceLocationId { get; set; }

    /// <summary>目标库位（最终存储库位）</summary>
    [SugarColumn(ColumnName = "target_location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? TargetLocationId { get; set; }
}

// ============================================================
//  4. 下架单：从货架上取下货品
// ============================================================
[SugarTable("wms_takedown_order")]
public class TakeDownOrder : BaseEntity
{
    [SugarColumn(ColumnName = "takedown_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string TakeDownNo { get; set; } = string.Empty;

    /// <summary>关联出库单 Id</summary>
    [SugarColumn(ColumnName = "outbound_order_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? OutboundOrderId { get; set; }

    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>状态：0=待下架, 1=下架中, 2=已下架, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<TakeDownOrderDetail> Details { get; set; } = [];
}

[SugarTable("wms_takedown_order_detail")]
public class TakeDownOrderDetail : WmsDocumentDetailBase
{
    [SugarColumn(ColumnName = "qty", IsNullable = false)]
    public decimal Qty { get; set; }

    /// <summary>来源库位（存储库位）</summary>
    [SugarColumn(ColumnName = "source_location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? SourceLocationId { get; set; }

    /// <summary>目标库位（暂存/分拣库位）</summary>
    [SugarColumn(ColumnName = "target_location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? TargetLocationId { get; set; }
}

// ============================================================
//  5. 拣货单：按出库需求生成拣货任务
// ============================================================
[SugarTable("wms_pick_order")]
public class PickOrder : BaseEntity
{
    [SugarColumn(ColumnName = "pick_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string PickNo { get; set; } = string.Empty;

    /// <summary>关联出库单 Id</summary>
    [SugarColumn(ColumnName = "outbound_order_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? OutboundOrderId { get; set; }

    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>状态：0=待拣货, 1=拣货中, 2=已拣货, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<PickOrderDetail> Details { get; set; } = [];
}

[SugarTable("wms_pick_order_detail")]
public class PickOrderDetail : WmsDocumentDetailBase
{
    /// <summary>应拣数量</summary>
    [SugarColumn(ColumnName = "qty", IsNullable = false)]
    public decimal Qty { get; set; }

    /// <summary>已拣数量</summary>
    [SugarColumn(ColumnName = "picked_qty", IsNullable = false)]
    public decimal PickedQty { get; set; }

    /// <summary>拣货库位</summary>
    [SugarColumn(ColumnName = "location_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR(26)")]
    public string? LocationId { get; set; }
}

// ============================================================
//  6. 出库单：客户订单出库
// ============================================================
[SugarTable("wms_outbound_order")]
public class OutboundOrder : BaseEntity
{
    [SugarColumn(ColumnName = "outbound_no", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string OutboundNo { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "warehouse_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string WarehouseId { get; set; } = string.Empty;

    /// <summary>客户</summary>
    [SugarColumn(ColumnName = "customer", Length = 200, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Customer { get; set; }

    /// <summary>预计出库时间</summary>
    [SugarColumn(ColumnName = "expected_ship_time", IsNullable = true)]
    public DateTimeOffset? ExpectedShipTime { get; set; }

    /// <summary>实际出库时间</summary>
    [SugarColumn(ColumnName = "actual_ship_time", IsNullable = true)]
    public DateTimeOffset? ActualShipTime { get; set; }

    [SugarColumn(ColumnName = "total_qty", IsNullable = false)]
    public decimal TotalQty { get; set; }

    /// <summary>总金额</summary>
    [SugarColumn(ColumnName = "total_amount", IsNullable = true)]
    public decimal? TotalAmount { get; set; }

    /// <summary>状态：0=待出库, 1=拣货中, 2=已出库, 3=已取消</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 0;

    [SugarColumn(ColumnName = "operator", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Operator { get; set; }

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }

    /// <summary>明细列表（导航属性，不映射到数据库）</summary>
    [SugarColumn(IsIgnore = true)]
    public List<OutboundOrderDetail> Details { get; set; } = [];
}

[SugarTable("wms_outbound_order_detail")]
public class OutboundOrderDetail : WmsDocumentDetailBase
{
    /// <summary>应发数量</summary>
    [SugarColumn(ColumnName = "expected_qty", IsNullable = false)]
    public decimal ExpectedQty { get; set; }

    /// <summary>实发数量</summary>
    [SugarColumn(ColumnName = "shipped_qty", IsNullable = false)]
    public decimal ShippedQty { get; set; }

    /// <summary>单价</summary>
    [SugarColumn(ColumnName = "unit_price", IsNullable = true)]
    public decimal? UnitPrice { get; set; }
}
