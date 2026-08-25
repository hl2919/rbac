using RbacWebApi.DTOs;

namespace RbacWebApi.Services.Cloud;

/// <summary>
/// 云盘管理服务：开通、查询状态
/// </summary>
public interface ICloudDiskService
{
    /// <summary>开通云盘：创建开通记录 + 动态建用户文件表</summary>
    Task<(bool Success, string Message)> ActivateAsync(string userId, ActivateCloudDiskRequest request);

    /// <summary>查询云盘状态</summary>
    Task<CloudDiskStatusResponse?> GetStatusAsync(string userId);

    /// <summary>检查用户是否已开通云盘</summary>
    Task<bool> IsActivatedAsync(string userId);

    /// <summary>获取用户文件表名</summary>
    string GetUserFileTableName(string userId);
}
