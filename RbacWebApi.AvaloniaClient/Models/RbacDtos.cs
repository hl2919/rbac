namespace RbacWebApi.AvaloniaClient.Models;

public class SysUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class SysRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

public class SysApiDto
{
    public string Id { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public bool NeedAuth { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}
