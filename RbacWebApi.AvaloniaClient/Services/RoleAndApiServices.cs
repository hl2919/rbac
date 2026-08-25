using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

public interface IRoleService
{
    Task<(bool Success, string Message, PageResponse<SysRoleDto>? Data)> GetListAsync(PageKeyRequest request);
    Task<(bool Success, string Message)> AssignUserAsync(string roleId, string userId);
    Task<(bool Success, string Message, List<string>? Data)> GetPermissionsAsync(string roleId);
    Task<(bool Success, string Message)> SetPermissionsAsync(string roleId, List<string> apiIds);
}

public class RoleService : IRoleService
{
    private readonly ApiClient _api;
    public RoleService(ApiClient api) => _api = api;

    public async Task<(bool Success, string Message, PageResponse<SysRoleDto>? Data)> GetListAsync(PageKeyRequest request)
    {
        var qs = $"pageIndex={request.PageIndex}&pageSize={request.PageSize}&keyword={Uri.EscapeDataString(request.Keyword ?? "")}";
        return await _api.GetAsync<PageResponse<SysRoleDto>>($"api/role/list?{qs}");
    }

    public Task<(bool Success, string Message)> AssignUserAsync(string roleId, string userId)
        => _api.PostNoResultAsync<object>($"api/role/{roleId}/assign/{userId}", new { });

    public async Task<(bool Success, string Message, List<string>? Data)> GetPermissionsAsync(string roleId)
    {
        var (ok, msg, data) = await _api.GetAsync<List<string>>($"api/role/{roleId}/permissions");
        return (ok, msg, data);
    }

    public Task<(bool Success, string Message)> SetPermissionsAsync(string roleId, List<string> apiIds)
        => _api.PutNoResultAsync($"api/role/{roleId}/permissions", apiIds);
}

public interface IApiResourceService
{
    Task<(bool Success, string Message, PageResponse<SysApiDto>? Data)> GetListAsync(PageKeyRequest request);
}

public class ApiResourceService : IApiResourceService
{
    private readonly ApiClient _api;
    public ApiResourceService(ApiClient api) => _api = api;

    public async Task<(bool Success, string Message, PageResponse<SysApiDto>? Data)> GetListAsync(PageKeyRequest request)
    {
        var qs = $"pageIndex={request.PageIndex}&pageSize={request.PageSize}&keyword={Uri.EscapeDataString(request.Keyword ?? "")}";
        return await _api.GetAsync<PageResponse<SysApiDto>>($"api/api/list?{qs}");
    }
}
