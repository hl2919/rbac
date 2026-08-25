using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// 用户角色关联表
/// </summary>
[SugarTable("sys_user_role")]
public class SysUserRole : BaseEntity
{
    [SugarColumn(ColumnName = "user_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR")]
    public string UserId { get; set; }

    [SugarColumn(ColumnName = "role_id", Length = 26, IsNullable = false, ColumnDataType = "VARCHAR")]
    public string RoleId { get; set; }
}
