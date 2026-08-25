using RbacWebApi.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.Services;

public interface IApiService
{
    /// <summary>分页查询 API 列表：固定参数页码/页大小 + 可选关键词</summary>
    Task<PageResponse<SysApi>> GetApiListAsync(PageKeyRequest request);

    Task<SysApi?> GetApiByIdAsync(string id);
    Task<(bool Success, string Message)> CreateApiAsync(SysApi api);
    Task<(bool Success, string Message)> UpdateApiAsync(SysApi api);
    Task<(bool Success, string Message)> DeleteApiAsync(string id);
}
