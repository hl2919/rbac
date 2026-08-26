using System.ComponentModel.DataAnnotations;

namespace RbacWebApi.DTOs;

// ============================================================
//  WMS 通用查询请求
// ============================================================

/// <summary>带可选关键词和状态筛选的分页请求</summary>
public class WmsQueryRequest : PageKeyRequest
{
    /// <summary>状态过滤（null 表示全部）</summary>
    public int? Status { get; set; }

    /// <summary>父级 ID 过滤（库区按仓库、巷道按库区、货架按巷道、库位按货架）</summary>
    public string? ParentId { get; set; }

    /// <summary>仓库 ID 过滤（业务单据使用）</summary>
    public string? WarehouseId { get; set; }
}

// ============================================================
//  WMS 基础数据 DTO：与实体字段一致，便于客户端使用
// ============================================================

public class WarehouseDto
{
    public string Id { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class ZoneDto
{
    public string Id { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public int ZoneType { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class AisleDto
{
    public string Id { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public string AisleCode { get; set; } = string.Empty;
    public string AisleName { get; set; } = string.Empty;
    public int Direction { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class RackDto
{
    public string Id { get; set; } = string.Empty;
    public string AisleId { get; set; } = string.Empty;
    public string RackCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int Levels { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class LocationDto
{
    public string Id { get; set; } = string.Empty;
    public string RackId { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int RowNo { get; set; }
    public int ColumnNo { get; set; }
    public int LevelNo { get; set; }
    public int LocationType { get; set; }
    public int Capacity { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Volume { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class ContainerDto
{
    public string Id { get; set; } = string.Empty;
    public string ContainerCode { get; set; } = string.Empty;
    public string? ContainerType { get; set; }
    public int Capacity { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

// ============================================================
//  WMS 业务单据 DTO：主表 + 明细表统一返回
// ============================================================

public class ReceiveOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string ReceiveNo { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string? Supplier { get; set; }
    public DateTimeOffset? ExpectedArrivalTime { get; set; }
    public DateTimeOffset? ActualArrivalTime { get; set; }
    public decimal TotalQty { get; set; }
    public decimal? TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<ReceiveOrderDetailDto> Details { get; set; } = [];
}

public class ReceiveOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal ExpectedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Remark { get; set; }
}

public class InboundOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string InboundNo { get; set; } = string.Empty;
    public string? ReceiveOrderId { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public decimal TotalQty { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<InboundOrderDetailDto> Details { get; set; } = [];
}

public class InboundOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal InboundQty { get; set; }
    public string? LocationId { get; set; }
    public string? Remark { get; set; }
}

public class PutawayOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string PutawayNo { get; set; } = string.Empty;
    public string? InboundOrderId { get; set; }
    public decimal TotalQty { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<PutawayOrderDetailDto> Details { get; set; } = [];
}

public class PutawayOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal Qty { get; set; }
    public string? SourceLocationId { get; set; }
    public string? TargetLocationId { get; set; }
    public string? Remark { get; set; }
}

public class TakeDownOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string TakeDownNo { get; set; } = string.Empty;
    public string? OutboundOrderId { get; set; }
    public decimal TotalQty { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<TakeDownOrderDetailDto> Details { get; set; } = [];
}

public class TakeDownOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal Qty { get; set; }
    public string? SourceLocationId { get; set; }
    public string? TargetLocationId { get; set; }
    public string? Remark { get; set; }
}

public class PickOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string PickNo { get; set; } = string.Empty;
    public string? OutboundOrderId { get; set; }
    public decimal TotalQty { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<PickOrderDetailDto> Details { get; set; } = [];
}

public class PickOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal Qty { get; set; }
    public decimal PickedQty { get; set; }
    public string? LocationId { get; set; }
    public string? Remark { get; set; }
}

public class OutboundOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OutboundNo { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string? Customer { get; set; }
    public DateTimeOffset? ExpectedShipTime { get; set; }
    public DateTimeOffset? ActualShipTime { get; set; }
    public decimal TotalQty { get; set; }
    public decimal? TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Operator { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
    public List<OutboundOrderDetailDto> Details { get; set; } = [];
}

public class OutboundOrderDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MasterId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public decimal ExpectedQty { get; set; }
    public decimal ShippedQty { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Remark { get; set; }
}

/// <summary>通用分页响应</summary>
public class WmsListResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
}
