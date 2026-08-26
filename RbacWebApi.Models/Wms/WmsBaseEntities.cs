using SqlSugar;

namespace RbacWebApi.Models.Wms;

// ============================================================
//  基础数据：仓库 → 库区 → 巷道 → 货架 → 库位
//                + 货品 + 周转箱
// ============================================================

/// <summary>仓库：WMS 顶级实体</summary>
[SugarTable("wms_warehouse")]
public class Warehouse : BaseEntity
{
    [SugarColumn(ColumnName = "warehouse_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string WarehouseCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "warehouse_name", Length = 100, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string WarehouseName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "address", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Address { get; set; }

    /// <summary>联系人</summary>
    [SugarColumn(ColumnName = "contact", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Contact { get; set; }

    [SugarColumn(ColumnName = "phone", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Phone { get; set; }

    /// <summary>状态：0=禁用, 1=启用</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }
}

/// <summary>库区：属于仓库，按功能划分（存储/分拣/收货/退货）</summary>
[SugarTable("wms_zone")]
public class Zone : BaseEntity
{
    [SugarColumn(ColumnName = "warehouse_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string WarehouseId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "zone_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ZoneCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "zone_name", Length = 100, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>库区类型：1=存储区, 2=分拣区, 3=收货区, 4=退货区</summary>
    [SugarColumn(ColumnName = "zone_type", IsNullable = false)]
    public int ZoneType { get; set; } = 1;

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;

    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Remark { get; set; }
}

/// <summary>库区巷道：库区下平行通道，货架沿巷道排列</summary>
[SugarTable("wms_aisle")]
public class Aisle : BaseEntity
{
    [SugarColumn(ColumnName = "zone_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string ZoneId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "aisle_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string AisleCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "aisle_name", Length = 100, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string AisleName { get; set; } = string.Empty;

    /// <summary>巷道方向：1=南北, 2=东西</summary>
    [SugarColumn(ColumnName = "direction", IsNullable = false)]
    public int Direction { get; set; } = 1;

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}

/// <summary>货架：位于巷道两侧，由行/列/层三维结构组成</summary>
[SugarTable("wms_rack")]
public class Rack : BaseEntity
{
    [SugarColumn(ColumnName = "aisle_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string AisleId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "rack_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string RackCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "rack_name", Length = 100, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string RackName { get; set; } = string.Empty;

    /// <summary>行数</summary>
    [SugarColumn(ColumnName = "rows", IsNullable = false)]
    public int Rows { get; set; } = 1;

    /// <summary>列数</summary>
    [SugarColumn(ColumnName = "columns", IsNullable = false)]
    public int Columns { get; set; } = 1;

    /// <summary>层数</summary>
    [SugarColumn(ColumnName = "levels", IsNullable = false)]
    public int Levels { get; set; } = 1;

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}

/// <summary>库位：货架上的最小存储单元，由 行/列/层 定位</summary>
[SugarTable("wms_location")]
public class Location : BaseEntity
{
    [SugarColumn(ColumnName = "rack_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR(26)")]
    public string RackId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "location_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>行号（从 1 开始）</summary>
    [SugarColumn(ColumnName = "row_no", IsNullable = false)]
    public int RowNo { get; set; }

    /// <summary>列号</summary>
    [SugarColumn(ColumnName = "column_no", IsNullable = false)]
    public int ColumnNo { get; set; }

    /// <summary>层号</summary>
    [SugarColumn(ColumnName = "level_no", IsNullable = false)]
    public int LevelNo { get; set; }

    /// <summary>库位类型：1=普通, 2=冷冻, 3=恒温, 4=贵重品</summary>
    [SugarColumn(ColumnName = "location_type", IsNullable = false)]
    public int LocationType { get; set; } = 1;

    /// <summary>容量限制（件数）</summary>
    [SugarColumn(ColumnName = "capacity", IsNullable = false)]
    public int Capacity { get; set; } = 100;

    /// <summary>状态：0=禁用, 1=空闲, 2=占用, 3=锁定</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}

/// <summary>货品：SKU 主数据</summary>
[SugarTable("wms_product")]
public class Product : BaseEntity
{
    [SugarColumn(ColumnName = "product_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ProductCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "product_name", Length = 200, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ProductName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "barcode", Length = 100, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Barcode { get; set; }

    /// <summary>分类</summary>
    [SugarColumn(ColumnName = "category", Length = 100, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Category { get; set; }

    /// <summary>规格</summary>
    [SugarColumn(ColumnName = "spec", Length = 200, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Spec { get; set; }

    /// <summary>计量单位</summary>
    [SugarColumn(ColumnName = "unit", Length = 20, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Unit { get; set; }

    /// <summary>重量(kg)</summary>
    [SugarColumn(ColumnName = "weight", IsNullable = true)]
    public decimal? Weight { get; set; }

    /// <summary>体积(m³)</summary>
    [SugarColumn(ColumnName = "volume", IsNullable = true)]
    public decimal? Volume { get; set; }

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}

/// <summary>周转箱：可循环使用的容器</summary>
[SugarTable("wms_container")]
public class Container : BaseEntity
{
    [SugarColumn(ColumnName = "container_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ContainerCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "container_type", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? ContainerType { get; set; }

    /// <summary>容量（件数）</summary>
    [SugarColumn(ColumnName = "capacity", IsNullable = false)]
    public int Capacity { get; set; } = 50;

    /// <summary>状态：0=禁用, 1=空闲, 2=使用中, 3=损坏</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}
