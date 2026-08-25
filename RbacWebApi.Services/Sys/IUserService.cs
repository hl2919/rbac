using RbacWebApi.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.Services;

public interface IUserService
{
    Task<(bool Success, string Message, LoginResponse? Response)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
    Task<SysUser?> GetUserByIdAsync(string userId);

    /// <summary>分页查询用户列表：固定参数页码/页大小 + 可选关键词</summary>
    Task<PageResponse<SysUser>> GetUserListAsync(PageKeyRequest request);

    Task<(bool Success, string Message)> CreateUserAsync(RegisterRequest request);
    Task<(bool Success, string Message)> UpdateUserAsync(string id, RegisterRequest request);
    Task<(bool Success, string Message)> DeleteUserAsync(string id);
    Task<(bool Success, string Message)> AssignRoleAsync(string userId, string roleId);
}
