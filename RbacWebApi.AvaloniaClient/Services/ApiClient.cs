using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

/// <summary>
/// 基础 API 客户端封装：统一处理 BaseUrl、JWT Header、ApiResponse 解包
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;
    private string? _token;

    public ApiClient(ApiSettings settings)
    {
        _settings = settings;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + "/")
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void SetToken(string? token)
    {
        _token = token;
        if (string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public HttpClient HttpClient => _http;
    public ApiSettings Settings => _settings;

    // ============================================================
    //  基础请求封装
    // ============================================================

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<(bool Success, string Message, T? Data)> GetAsync<T>(string url)
    {
        try
        {
            var resp = await _http.GetAsync(url);
            return await UnwrapResponse<T>(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", default);
        }
    }

    public async Task<(bool Success, string Message, T? Data)> PostAsync<T, TBody>(string url, TBody body)
    {
        try
        {
            var content = JsonContent.Create(body, options: JsonOpts);
            var resp = await _http.PostAsync(url, content);
            return await UnwrapResponse<T>(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", default);
        }
    }

    public async Task<(bool Success, string Message)> PostNoResultAsync<TBody>(string url, TBody body)
    {
        var (ok, msg, _) = await PostAsync<object, TBody>(url, body);
        return (ok, msg);
    }

    public async Task<(bool Success, string Message, T? Data)> PutAsync<T, TBody>(string url, TBody body)
    {
        try
        {
            var content = JsonContent.Create(body, options: JsonOpts);
            var resp = await _http.PutAsync(url, content);
            return await UnwrapResponse<T>(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", default);
        }
    }

    public async Task<(bool Success, string Message)> PutNoResultAsync<TBody>(string url, TBody body)
    {
        var (ok, msg, _) = await PutAsync<object, TBody>(url, body);
        return (ok, msg);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string url)
    {
        try
        {
            var resp = await _http.DeleteAsync(url);
            var (ok, msg, _) = await UnwrapResponse<object>(resp);
            return (ok, msg);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}");
        }
    }

    // ============================================================
    //  运行期类型化请求：返回 JsonElement，由调用方按运行时 Type 反序列化
    //  适用于 WMS 这类多实体、类型在运行期才确定的场景
    // ============================================================

    public async Task<(bool Success, string Message, JsonElement? Data)> GetRawAsync(string url)
    {
        try
        {
            var resp = await _http.GetAsync(url);
            return await UnwrapRaw(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, JsonElement? Data)> PostRawAsync(string url, string jsonBody)
    {
        try
        {
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, content);
            return await UnwrapRaw(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, JsonElement? Data)> PutRawAsync(string url, string jsonBody)
    {
        try
        {
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync(url, content);
            return await UnwrapRaw(resp);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteNoResultAsync(string url)
    {
        try
        {
            var resp = await _http.DeleteAsync(url);
            var (ok, msg, _) = await UnwrapRaw(resp);
            return (ok, msg);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}");
        }
    }

    /// <summary>ApiResponse&lt;JsonElement&gt; 解包：Data 以 JsonElement 形式返回，交由调用方按运行时类型反序列化</summary>
    private async Task<(bool Success, string Message, JsonElement? Data)> UnwrapRaw(HttpResponseMessage resp)
    {
        var raw = await resp.Content.ReadAsStringAsync();
        try
        {
            var api = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(raw, JsonOpts);
            if (api == null)
                return (false, "响应解析失败", null);
            if (api.Code == 200)
                return (true, api.Message, api.Data);
            return (false, string.IsNullOrEmpty(api.Message) ? $"错误码 {api.Code}" : api.Message, api.Data);
        }
        catch (JsonException)
        {
            return (false, $"响应格式异常: {raw[..Math.Min(200, raw.Length)]}", null);
        }
    }

    private async Task<(bool Success, string Message, T? Data)> UnwrapResponse<T>(HttpResponseMessage resp)
    {
        var raw = await resp.Content.ReadAsStringAsync();
        try
        {
            var api = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOpts);
            if (api == null)
                return (false, "响应解析失败", default);
            if (api.Code == 200)
                return (true, api.Message, api.Data);
            return (false, string.IsNullOrEmpty(api.Message) ? $"错误码 {api.Code}" : api.Message, api.Data);
        }
        catch (JsonException)
        {
            return (false, $"响应格式异常: {raw[..Math.Min(200, raw.Length)]}", default);
        }
    }
}
