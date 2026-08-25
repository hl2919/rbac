using SqlSugar;

namespace RbacWebApi.Models;

/// <summary>
/// 用户表
/// </summary>
[SugarTable("sys_user")]
public class SysUser : BaseEntity
{
    [SugarColumn(ColumnName = "username", Length = 50, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string Username { get; set; }

    [SugarColumn(ColumnName = "password_hash", Length = 200, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string PasswordHash { get; set; }

    [SugarColumn(ColumnName = "nickname", Length = 50, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Nickname { get; set; }

    [SugarColumn(ColumnName = "email", Length = 100, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Email { get; set; }

    [SugarColumn(ColumnName = "phone", Length = 20, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Phone { get; set; }

    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;
}
