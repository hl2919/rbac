using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

public interface IUserService
{
    Task<(bool Success, string Message, PageResponse<SysUserDto>? Data)> GetListAsync(PageKeyRequest request);
    Task<(bool Success, string Message, SysUserDto? Data)> GetByIdAsync(string id);
    Task<(bool Success, string Message)> CreateAsync(RegisterRequest request);
    Task<(bool Success, string Message)> UpdateAsync(string id, RegisterRequest request);
    Task<(bool Success, string Message)> DeleteAsync(string id);
}

public class UserService : IUserService
{
    private readonly ApiClient _api;
    public UserService(ApiClient api) => _api = api;

    public async Task<(bool Success, string Message, PageResponse<SysUserDto>? Data)> GetListAsync(PageKeyRequest request)
    {
        var qs = $"pageIndex={request.PageIndex}&pageSize={request.PageSize}&keyword={Uri.EscapeDataString(request.Keyword ?? "")}";
        return await _api.GetAsync<PageResponse<SysUserDto>>($"api/user/list?{qs}");
    }

    public Task<(bool Success, string Message, SysUserDto? Data)> GetByIdAsync(string id)
        => _api.GetAsync<SysUserDto>($"api/user/{id}");

    public Task<(bool Success, string Message)> CreateAsync(RegisterRequest request)
        => _api.PostNoResultAsync("api/user", request);

    public Task<(bool Success, string Message)> UpdateAsync(string id, RegisterRequest request)
        => _api.PutNoResultAsync($"api/user/{id}", request);

    public Task<(bool Success, string Message)> DeleteAsync(string id)
        => _api.DeleteAsync($"api/user/{id}");
}
