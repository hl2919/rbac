using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

public interface IAuthService
{
    CurrentUser? Current { get; }
    event Action? StateChanged;

    Task<(bool Success, string Message)> LoginAsync(string username, string password);
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message)> LoadSavedSessionAsync();
    void Logout();

    /// <summary>检查本地是否有保存的会话</summary>
    bool HasSavedSession();
}

public class AuthService : IAuthService
{
    private readonly ApiClient _api;

    public CurrentUser? Current { get; private set; }
    public event Action? StateChanged;

    public AuthService(ApiClient api)
    {
        _api = api;
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        var (ok, msg, data) = await _api.PostAsync<LoginResponse, LoginRequest>(
            "api/auth/login",
            new LoginRequest { Username = username, Password = password });

        if (!ok || data == null)
            return (false, msg);

        Current = new CurrentUser
        {
            UserId = data.UserId,
            Username = data.Username,
            Nickname = data.Nickname,
            Roles = data.Roles,
            Token = data.Token,
            ExpiresAt = data.ExpiresAt
        };
        _api.SetToken(data.Token);
        TokenStorage.Save(new TokenStorage.StoredToken(data.Token, data.UserId, data.Username, data.ExpiresAt));
        StateChanged?.Invoke();
        return (true, msg);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        var (ok, msg) = await _api.PostNoResultAsync("api/auth/register", request);
        return (ok, msg);
    }

    public async Task<(bool Success, string Message)> LoadSavedSessionAsync()
    {
        var stored = TokenStorage.Load();
        if (stored == null) return (false, "无缓存登录态");
        if (stored.ExpiresAt <= DateTime.Now)
        {
            TokenStorage.Clear();
            return (false, "登录态已过期");
        }

        _api.SetToken(stored.Token);
        // 调用 /api/auth/me 确认 token 有效并获取角色
        var (ok, _, _) = await _api.GetAsync<object>("api/auth/me");
        if (!ok)
        {
            _api.SetToken(null);
            TokenStorage.Clear();
            return (false, "Token 已失效");
        }

        Current = new CurrentUser
        {
            UserId = stored.UserId,
            Username = stored.Username,
            Token = stored.Token,
            ExpiresAt = stored.ExpiresAt
        };
        StateChanged?.Invoke();
        return (true, "已恢复登录态");
    }

    public void Logout()
    {
        Current = null;
        _api.SetToken(null);
        TokenStorage.Clear();
        StateChanged?.Invoke();
    }

    public bool HasSavedSession()
    {
        var stored = TokenStorage.Load();
        return stored != null && stored.ExpiresAt > DateTime.Now;
    }
}
