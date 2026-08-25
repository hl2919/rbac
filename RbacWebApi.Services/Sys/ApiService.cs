using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Repositories;

namespace RbacWebApi.Services;

public class ApiService : IApiService
{
    private readonly IApiRepository _apiRepo;
    private readonly IRoleApiRepository _roleApiRepo;

    public ApiService(IApiRepository apiRepo, IRoleApiRepository roleApiRepo)
    {
        _apiRepo = apiRepo;
        _roleApiRepo = roleApiRepo;
    }

    public Task<PageResponse<SysApi>> GetApiListAsync(PageKeyRequest request)
    {
        var keyword = request.Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return _apiRepo.GetPagedListAsync(_ => true, request, a => a.CreateTime, SqlSugar.OrderByType.Asc);
        }
        return _apiRepo.GetPagedListAsync(
            a => a.ApiName.Contains(keyword) || a.ApiUrl.Contains(keyword)
                 || a.RequestMethod.Contains(keyword)
                 || (a.Description != null && a.Description.Contains(keyword)),
            request,
            a => a.CreateTime,
            SqlSugar.OrderByType.Asc);
    }

    public Task<SysApi?> GetApiByIdAsync(string id)
    {
        return _apiRepo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message)> CreateApiAsync(SysApi api)
    {
        if (await _apiRepo.AnyAsync(a => a.ApiUrl == api.ApiUrl && a.RequestMethod == api.RequestMethod))
        {
            return (false, "该URL和方法的API已存在");
        }
        await _apiRepo.InsertAsync(api);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateApiAsync(SysApi api)
    {
        var existing = await _apiRepo.GetByIdAsync(api.Id);
        if (existing == null)
        {
            return (false, "API不存在");
        }
        if (await _apiRepo.AnyAsync(a => a.ApiUrl == api.ApiUrl && a.RequestMethod == api.RequestMethod && a.Id != api.Id))
        {
            return (false, "该URL和方法的API已存在");
        }
        // LastUpdateTime 由 AOP 自动填充
        await _apiRepo.UpdateAsync(api);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteApiAsync(string id)
    {
        if (!await _apiRepo.AnyAsync(a => a.Id == id))
        {
            return (false, "API不存在");
        }
        await _apiRepo.DeleteByIdAsync(id);
        await _roleApiRepo.DeleteBatchAsync(ra => ra.ApiId == id);
        return (true, "删除成功");
    }
}
