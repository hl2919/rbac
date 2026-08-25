using SqlSugar;

namespace RbacWebApi.Models.Cloud;

/// <summary>
/// 用户云盘开通记录表
/// </summary>
[SugarTable("user_cloud_disk")]
public class UserCloudDisk : BaseEntity
{
    /// <summary>用户 ID</summary>
    [SugarColumn(ColumnName = "user_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>存储配额（字节），默认 10GB</summary>
    [SugarColumn(ColumnName = "quota", IsNullable = false)]
    public long Quota { get; set; } = 10L * 1024 * 1024 * 1024;

    /// <summary>已使用空间（字节）</summary>
    [SugarColumn(ColumnName = "used_size", IsNullable = false)]
    public long UsedSize { get; set; } = 0;

    /// <summary>状态：0=已禁用, 1=正常</summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}
