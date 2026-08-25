using RbacWebApi.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.Services;

public interface IRoleService
{
    /// <summary>分页查询角色列表：固定参数页码/页大小 + 可选关键词</summary>
    Task<PageResponse<SysRole>> GetRoleListAsync(PageKeyRequest request);

    Task<SysRole?> GetRoleByIdAsync(string id);
    Task<(bool Success, string Message)> CreateRoleAsync(SysRole role);
    Task<(bool Success, string Message)> UpdateRoleAsync(SysRole role);
    Task<(bool Success, string Message)> DeleteRoleAsync(string id);
}
