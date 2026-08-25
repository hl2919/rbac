using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// 实体基类：主键 Ulid(字符串存储) + 系统字段 CreateTime / LastUpdateTime
/// 值由 DbContext 中的 DataExecuting AOP 自动填充
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键，Ulid 字符串（26字符），AOP 在插入时自动生成
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false, ColumnName = "id", ColumnDataType = "VARCHAR(26)")]
    public string Id { get; set; }

    /// <summary>
    /// 创建时间，AOP 在插入时自动填充
    /// </summary>
    [SugarColumn(ColumnName = "create_time", IsNullable = false)]
    public DateTimeOffset CreateTime { get; set; }

    /// <summary>
    /// 最后更新时间，AOP 在插入/更新时自动填充
    /// </summary>
    [SugarColumn(ColumnName = "last_update_time", IsNullable = true)]
    public DateTimeOffset? LastUpdateTime { get; set; }
}
