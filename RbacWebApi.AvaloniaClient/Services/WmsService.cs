using System.Collections;
using System.Text.Json;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

/// <summary>
/// WMS 客户端服务：单实体类型在运行期才确定（13 类基础数据/单据），
/// 故采用 ApiClient 的 Raw 方法 + 运行时 Type 反序列化，避免为每类写一套接口。
/// </summary>
public interface IWmsService
{
    /// <summary>分页查询：返回 IList（元素类型为 itemType）+ Total</summary>
    Task<(bool Success, string Message, IList? Items, int Total)> GetListAsync(Type itemType, string resource, WmsQueryRequest request);

    /// <summary>按 Id 查询单条</summary>
    Task<(bool Success, string Message, object? Data)> GetAsync(Type itemType, string resource, string id);

    /// <summary>新增</summary>
    Task<(bool Success, string Message)> CreateAsync(string resource, object body);

    /// <summary>更新</summary>
    Task<(bool Success, string Message)> UpdateAsync(string resource, string id, object body);

    /// <summary>删除</summary>
    Task<(bool Success, string Message)> DeleteAsync(string resource, string id);

    /// <summary>查询主表 + 明细（业务单据）</summary>
    Task<(bool Success, string Message, object? Data)> GetWithDetailsAsync(Type itemType, string resource, string id);
}

public class WmsService : IWmsService
{
    private readonly ApiClient _api;
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WmsService(ApiClient api) => _api = api;

    public async Task<(bool Success, string Message, IList? Items, int Total)> GetListAsync(Type itemType, string resource, WmsQueryRequest request)
    {
        var qs = $"pageIndex={request.PageIndex}&pageSize={request.PageSize}"
               + $"&keyword={Uri.EscapeDataString(request.Keyword ?? "")}"
               + (request.Status.HasValue ? $"&status={request.Status}" : "")
               + (string.IsNullOrEmpty(request.ParentId) ? "" : $"&parentId={Uri.EscapeDataString(request.ParentId)}")
               + (string.IsNullOrEmpty(request.WarehouseId) ? "" : $"&warehouseId={Uri.EscapeDataString(request.WarehouseId)}");

        var (ok, msg, data) = await _api.GetRawAsync($"api/{resource}/list?{qs}");
        if (!ok || data == null || data.Value.ValueKind == JsonValueKind.Null)
            return (ok, msg, null, 0);

        var listType = typeof(WmsListResponse<>).MakeGenericType(itemType);
        var resp = JsonSerializer.Deserialize(data.Value.GetRawText(), listType, Opt);
        if (resp == null) return (false, "列表解析失败", null, 0);

        var items = (IList?)listType.GetProperty(nameof(WmsListResponse<object>.Items))?.GetValue(resp);
        var total = (int?)listType.GetProperty(nameof(WmsListResponse<object>.Total))?.GetValue(resp) ?? 0;
        return (true, msg, items, total);
    }

    public async Task<(bool Success, string Message, object? Data)> GetAsync(Type itemType, string resource, string id)
    {
        var (ok, msg, data) = await _api.GetRawAsync($"api/{resource}/{Uri.EscapeDataString(id)}");
        if (!ok || data == null || data.Value.ValueKind == JsonValueKind.Null)
            return (ok, msg, null);
        return (true, msg, JsonSerializer.Deserialize(data.Value.GetRawText(), itemType, Opt));
    }

    public async Task<(bool Success, string Message, object? Data)> GetWithDetailsAsync(Type itemType, string resource, string id)
    {
        var (ok, msg, data) = await _api.GetRawAsync($"api/{resource}/full/{Uri.EscapeDataString(id)}");
        if (!ok || data == null || data.Value.ValueKind == JsonValueKind.Null)
            return (ok, msg, null);
        return (true, msg, JsonSerializer.Deserialize(data.Value.GetRawText(), itemType, Opt));
    }

    public async Task<(bool Success, string Message)> CreateAsync(string resource, object body)
    {
        var json = JsonSerializer.Serialize(body, body.GetType(), Opt);
        var (ok, msg, _) = await _api.PostRawAsync($"api/{resource}", json);
        return (ok, msg);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(string resource, string id, object body)
    {
        var json = JsonSerializer.Serialize(body, body.GetType(), Opt);
        var (ok, msg, _) = await _api.PutRawAsync($"api/{resource}/{Uri.EscapeDataString(id)}", json);
        return (ok, msg);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string resource, string id)
        => await _api.DeleteNoResultAsync($"api/{resource}/{Uri.EscapeDataString(id)}");
}
