using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// API接口权限表
/// </summary>
[SugarTable("sys_api")]
public class SysApi : BaseEntity
{
    [SugarColumn(ColumnName = "api_url", Length = 200, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ApiUrl { get; set; }

    [SugarColumn(ColumnName = "request_method", Length = 10, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string RequestMethod { get; set; }

    [SugarColumn(ColumnName = "api_name", Length = 100, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string ApiName { get; set; }

    [SugarColumn(ColumnName = "description", Length = 200, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "need_auth", IsNullable = false)]
    public bool NeedAuth { get; set; } = true;
}
