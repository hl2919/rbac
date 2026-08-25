using RbacWebApi.Models;

namespace RbacWebApi.Services;

public interface IPermissionService
{
    Task<bool> CheckApiPermissionAsync(string userId, string apiUrl, string requestMethod);
    Task<List<string>> GetUserRoleCodesAsync(string userId);
    Task<List<SysRole>> GetUserRolesAsync(string userId);
    Task<List<SysApi>> GetRoleApisAsync(string roleId);
    Task SetRoleApisAsync(string roleId, List<string> apiIds);
}
