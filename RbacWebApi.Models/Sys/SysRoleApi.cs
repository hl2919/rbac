using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// 角色API权限关联表
/// </summary>
[SugarTable("sys_role_api")]
public class SysRoleApi : BaseEntity
{
    [SugarColumn(ColumnName = "role_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR")]
    public string RoleId { get; set; }

    [SugarColumn(ColumnName = "api_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR")]
    public string ApiId { get; set; }
}
