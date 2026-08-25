using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// 角色表
/// </summary>
[SugarTable("sys_role")]
public class SysRole : BaseEntity
{
    [SugarColumn(ColumnName = "role_name", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string RoleName { get; set; }

    [SugarColumn(ColumnName = "role_code", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string RoleCode { get; set; }

    [SugarColumn(ColumnName = "description", Length = 200, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}
