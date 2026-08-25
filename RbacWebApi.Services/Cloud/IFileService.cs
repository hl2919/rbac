using RbacWebApi.DTOs;
using RbacWebApi.Models.Cloud;

namespace RbacWebApi.Services.Cloud;

/// <summary>
/// 文件服务：秒传、分片上传、断点续传下载、文件列表、文件夹管理
/// </summary>
public interface IFileService
{
    /// <summary>上传初始化：判断秒传或开启分片上传</summary>
    Task<(bool Success, string Message, UploadInitResponse? Response)> UploadInitAsync(string userId, UploadInitRequest request);

    /// <summary>上传分片</summary>
    Task<(bool Success, string Message, UploadChunkResponse? Response)> UploadChunkAsync(string userId, string fileId, int chunkIndex, Stream chunkStream);

    /// <summary>上传完成：合并分片、写入系统文件表、移动到 ULID 目录</summary>
    Task<(bool Success, string Message, UploadCompleteResponse? Response)> UploadCompleteAsync(string userId, string fileId);

    /// <summary>新建文件夹</summary>
    Task<(bool Success, string Message, string? FolderId)> CreateFolderAsync(string userId, CreateFolderRequest request);

    /// <summary>分页查询用户文件/文件夹列表</summary>
    Task<PageResponse<UserFileInfoResponse>> GetUserFileListAsync(string userId, UserFileListRequest request);

    /// <summary>删除用户文件或文件夹（文件夹递归删除）</summary>
    Task<(bool Success, string Message)> DeleteFileAsync(string userId, string fileId);

    /// <summary>获取文件物理路径（下载时使用）</summary>
    Task<(SysFile? SysFile, UserFile? UserFile)> GetFileInfoForDownloadAsync(string userId, string fileId);
}
