using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Models;

/// <summary>WMS 实体类型描述：菜单标题 + 路由 + DTO 类型 + 是否业务单据</summary>
public record WmsEntityType
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    /// <summary>分组：基础数据 / 业务单据</summary>
    public string Group { get; init; } = string.Empty;
    /// <summary>API 路由前缀（不含 api/），如 warehouse、receive-order</summary>
    public string Resource { get; init; } = string.Empty;
    /// <summary>客户端 DTO 类型（运行期用于反序列化与表单生成）</summary>
    public Type ItemType { get; init; } = typeof(object);
    /// <summary>是否业务单据（含明细，更新时需保留明细）</summary>
    public bool IsDocument { get; init; }
}

/// <summary>WMS 模块 13 类实体目录：7 基础数据 + 6 业务单据</summary>
public static class WmsEntityCatalog
{
    public static readonly IReadOnlyList<WmsEntityType> All =
    [
        // 基础数据
        new WmsEntityType { Key = "warehouse",  Title = "仓库",   Group = "基础数据", Resource = "warehouse",  ItemType = typeof(WarehouseDto) },
        new WmsEntityType { Key = "zone",       Title = "库区",   Group = "基础数据", Resource = "zone",       ItemType = typeof(ZoneDto) },
        new WmsEntityType { Key = "aisle",      Title = "巷道",   Group = "基础数据", Resource = "aisle",      ItemType = typeof(AisleDto) },
        new WmsEntityType { Key = "rack",       Title = "货架",   Group = "基础数据", Resource = "rack",       ItemType = typeof(RackDto) },
        new WmsEntityType { Key = "location",   Title = "库位",   Group = "基础数据", Resource = "location",   ItemType = typeof(LocationDto) },
        new WmsEntityType { Key = "product",   Title = "货品",   Group = "基础数据", Resource = "product",     ItemType = typeof(ProductDto) },
        new WmsEntityType { Key = "container",  Title = "周转箱", Group = "基础数据", Resource = "container",  ItemType = typeof(ContainerDto) },
        // 业务单据
        new WmsEntityType { Key = "receive-order",  Title = "收货单", Group = "业务单据", Resource = "receive-order",  ItemType = typeof(ReceiveOrderDto),  IsDocument = true },
        new WmsEntityType { Key = "inbound-order",  Title = "入库单", Group = "业务单据", Resource = "inbound-order",  ItemType = typeof(InboundOrderDto),  IsDocument = true },
        new WmsEntityType { Key = "putaway-order",  Title = "上架单", Group = "业务单据", Resource = "putaway-order",  ItemType = typeof(PutawayOrderDto),  IsDocument = true },
        new WmsEntityType { Key = "takedown-order", Title = "下架单", Group = "业务单据", Resource = "takedown-order", ItemType = typeof(TakeDownOrderDto), IsDocument = true },
        new WmsEntityType { Key = "pick-order",     Title = "拣货单", Group = "业务单据", Resource = "pick-order",     ItemType = typeof(PickOrderDto),     IsDocument = true },
        new WmsEntityType { Key = "outbound-order", Title = "出库单", Group = "业务单据", Resource = "outbound-order", ItemType = typeof(OutboundOrderDto), IsDocument = true },
    ];
}
