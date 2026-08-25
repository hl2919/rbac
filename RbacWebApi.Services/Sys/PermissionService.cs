using RbacWebApi.Models;
using RbacWebApi.Repositories;

namespace RbacWebApi.Services;

public class PermissionService : IPermissionService
{
    private readonly IApiRepository _apiRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRoleApiRepository _roleApiRepo;

    public PermissionService(
        IApiRepository apiRepo,
        IUserRoleRepository userRoleRepo,
        IRoleRepository roleRepo,
        IRoleApiRepository roleApiRepo)
    {
        _apiRepo = apiRepo;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _roleApiRepo = roleApiRepo;
    }

    public async Task<bool> CheckApiPermissionAsync(string userId, string apiUrl, string requestMethod)
    {
        // 复杂查询直接走底层 SqlSugarClient，保持灵活
        var client = _apiRepo.Client;

        var api = await client.Queryable<SysApi>()
            .FirstAsync(a => a.ApiUrl == apiUrl && a.RequestMethod == requestMethod);

        if (api == null)
        {
            // 通配符匹配：{id} 等参数占位
            api = await FindApiByWildcardMatchAsync(apiUrl, requestMethod);
        }

        if (api == null)
        {
            // 未配置的 API：默认登录即放行
            return true;
        }

        if (!api.NeedAuth)
        {
            return true;
        }

        var userRoleIds = await client.Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!userRoleIds.Any())
        {
            return false;
        }

        var hasPermission = await client.Queryable<SysRoleApi>()
            .AnyAsync(ra => userRoleIds.Contains(ra.RoleId) && ra.ApiId == api.Id);

        return hasPermission;
    }

    private async Task<SysApi?> FindApiByWildcardMatchAsync(string requestUrl, string method)
    {
        var allApis = await _apiRepo.GetListAsync(a => a.RequestMethod == method);
        foreach (var api in allApis)
        {
            if (ApiUrlMatches(api.ApiUrl, requestUrl))
            {
                return api;
            }
        }
        return null;
    }

    private static bool ApiUrlMatches(string patternUrl, string requestUrl)
    {
        var patternParts = patternUrl.Trim('/').Split('/');
        var requestParts = requestUrl.Trim('/').Split('/');
        if (patternParts.Length != requestParts.Length) return false;
        for (var i = 0; i < patternParts.Length; i++)
        {
            var p = patternParts[i];
            var r = requestParts[i];
            if (p.StartsWith('{') && p.EndsWith('}')) continue;
            if (!string.Equals(p, r, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public async Task<List<string>> GetUserRoleCodesAsync(string userId)
    {
        return await _userRoleRepo.Client.Queryable<SysUserRole>()
            .LeftJoin<SysRole>((ur, r) => ur.RoleId == r.Id)
            .Where(ur => ur.UserId == userId)
            .Select((ur, r) => r.RoleCode)
            .ToListAsync();
    }

    public async Task<List<SysRole>> GetUserRolesAsync(string userId)
    {
        var roleIds = await _userRoleRepo.GetListAsync(ur => ur.UserId == userId);
        if (!roleIds.Any()) return [];
        var ids = roleIds.Select(ur => ur.RoleId).Distinct().ToList();
        return await _roleRepo.GetListAsync(r => ids.Contains(r.Id));
    }

    public async Task<List<SysApi>> GetRoleApisAsync(string roleId)
    {
        var apiIds = await _roleApiRepo.GetListAsync(ra => ra.RoleId == roleId);
        if (!apiIds.Any()) return [];
        var ids = apiIds.Select(ra => ra.ApiId).Distinct().ToList();
        return await _apiRepo.GetListAsync(a => ids.Contains(a.Id));
    }

    public async Task SetRoleApisAsync(string roleId, List<string> apiIds)
    {
        await _roleApiRepo.DeleteBatchAsync(ra => ra.RoleId == roleId);
        if (apiIds.Count == 0) return;
        var list = apiIds.Distinct().Select(apiId => new SysRoleApi { RoleId = roleId, ApiId = apiId }).ToList();
        await _roleApiRepo.InsertRangeAsync(list);
    }
}
