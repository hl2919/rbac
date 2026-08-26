using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.Services;

public interface ICloudDiskService
{
    Task<(bool Success, string Message)> ActivateAsync(long quotaGb = 10);
    Task<(bool Success, string Message, CloudDiskStatusResponse? Data)> GetStatusAsync();
}

public class CloudDiskService : ICloudDiskService
{
    private readonly ApiClient _api;
    public CloudDiskService(ApiClient api) => _api = api;

    public Task<(bool Success, string Message)> ActivateAsync(long quotaGb = 10)
        => _api.PostNoResultAsync("api/clouddisk/activate", new ActivateCloudDiskRequest
        {
            Quota = quotaGb * 1024L * 1024L * 1024L
        });

    public Task<(bool Success, string Message, CloudDiskStatusResponse? Data)> GetStatusAsync()
        => _api.GetAsync<CloudDiskStatusResponse>("api/clouddisk/status");
}

public interface IFileApiService
{
    Task<(bool Success, string Message, UploadInitResponse? Data)> UploadInitAsync(
        string fileName, long fileSize, string md5, string sha1, string? parentFolderId = null, long chunkSize = 5 * 1024 * 1024);

    Task<(bool Success, string Message, UploadChunkResponse? Data)> UploadChunkAsync(
        string fileId, int chunkIndex, Stream chunkStream);

    Task<(bool Success, string Message, UploadCompleteResponse? Data)> UploadCompleteAsync(string fileId);

    Task<(bool Success, string Message, string? FolderId)> CreateFolderAsync(string folderName, string? parentFolderId = null);

    Task<(bool Success, string Message, PageResponse<UserFileInfoResponse>? Data)> GetUserFileListAsync(
        string? parentFolderId = null, string? keyword = null, int pageIndex = 1, int pageSize = 50);

    Task<(bool Success, string Message)> DeleteAsync(string fileId);

    Task<(string? PhysicalUrl, string? FileName, string? ContentType)> GetDownloadInfoAsync(string fileId);

    /// <summary>调用后端 download/chunk 接口获取文件总大小（通过 X-Total-Size 头）</summary>
    Task<(bool Success, string Message, long Size)> GetDownloadSizeAsync(string fileId);

    /// <summary>
    /// 调用后端分块下载接口：GET /api/file/download/chunk/{id}?offset=X&length=Y
    /// 返回指定字节区间的流 + 实际接收字节数 + 文件总大小（来自 X-Total-Size 头）。
    /// 当 ActualSize == 0 表示已到文件末尾。
    /// </summary>
    Task<(bool Success, string Message, Stream? Stream, long Received, long TotalSize)> DownloadChunkAsync(
        string fileId, long offset, long length);
}

public class FileApiService : IFileApiService
{
    private readonly ApiClient _api;
    public FileApiService(ApiClient api) => _api = api;

    public Task<(bool Success, string Message, UploadInitResponse? Data)> UploadInitAsync(
        string fileName, long fileSize, string md5, string sha1, string? parentFolderId = null, long chunkSize = 5 * 1024 * 1024)
        => _api.PostAsync<UploadInitResponse, UploadInitRequest>("api/file/upload/init", new UploadInitRequest
        {
            FileName = fileName,
            FileSize = fileSize,
            Md5 = md5,
            Sha1 = sha1,
            ParentFolderId = parentFolderId,
            ChunkSize = chunkSize
        });

    public async Task<(bool Success, string Message, UploadChunkResponse? Data)> UploadChunkAsync(
        string fileId, int chunkIndex, Stream chunkStream)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(fileId), "fileId");
            content.Add(new StringContent(chunkIndex.ToString()), "chunkIndex");
            var sc = new StreamContent(chunkStream);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(sc, "file", "chunk");
            var resp = await _api.HttpClient.PostAsync("api/file/upload/chunk", content);
            var raw = await resp.Content.ReadAsStringAsync();
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            var api = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UploadChunkResponse>>(raw, opts);
            if (api == null) return (false, "响应解析失败", null);
            if (api.Code == 200) return (true, api.Message, api.Data);
            return (false, api.Message, null);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", null);
        }
    }

    public Task<(bool Success, string Message, UploadCompleteResponse? Data)> UploadCompleteAsync(string fileId)
        => _api.PostAsync<UploadCompleteResponse, object>("api/file/upload/complete", new { FileId = fileId });

    public async Task<(bool Success, string Message, string? FolderId)> CreateFolderAsync(
        string folderName, string? parentFolderId = null)
    {
        var (ok, msg, data) = await _api.PostAsync<string, CreateFolderRequest>("api/file/folder",
            new CreateFolderRequest { FolderName = folderName, ParentFolderId = parentFolderId });
        return (ok, msg, data);
    }

    public async Task<(bool Success, string Message, PageResponse<UserFileInfoResponse>? Data)> GetUserFileListAsync(
        string? parentFolderId = null, string? keyword = null, int pageIndex = 1, int pageSize = 50)
    {
        var qs = $"pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(parentFolderId))
            qs += $"&parentFolderId={Uri.EscapeDataString(parentFolderId)}";
        if (!string.IsNullOrEmpty(keyword))
            qs += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await _api.GetAsync<PageResponse<UserFileInfoResponse>>($"api/file/list?{qs}");
    }

    public Task<(bool Success, string Message)> DeleteAsync(string fileId)
        => _api.DeleteAsync($"api/file/{fileId}");

    public Task<(string? PhysicalUrl, string? FileName, string? ContentType)> GetDownloadInfoAsync(string fileId)
    {
        // 下载 URL 直接组合，由客户端通过 HTTP 带 JWT 下载（支持 Range 断点续传）
        var url = _api.Settings.BaseUrl.TrimEnd('/') + $"/api/file/download/{Uri.EscapeDataString(fileId)}";
        return Task.FromResult<(string?, string?, string?)>((url, null, null));
    }

    /// <summary>用 download/chunk 接口探测文件总大小（offset=0, length=1）</summary>
    public async Task<(bool Success, string Message, long Size)> GetDownloadSizeAsync(string fileId)
    {
        try
        {
            var url = $"api/file/download/chunk/{Uri.EscapeDataString(fileId)}?offset=0&length=1";
            using var resp = await _api.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
                return (false, $"服务器返回 {resp.StatusCode}", 0);

            // 优先 X-Total-Size 自定义头
            if (resp.Headers.TryGetValues("X-Total-Size", out var totalValues))
            {
                var totalStr = totalValues.FirstOrDefault();
                if (long.TryParse(totalStr, out var t) && t > 0)
                    return (true, "OK", t);
            }
            // 退回 Content-Length
            if (resp.Content.Headers.ContentLength.HasValue)
                return (true, "OK", resp.Content.Headers.ContentLength.Value);
            return (false, "服务器未返回文件大小", 0);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", 0);
        }
    }

    /// <summary>调用后端分块下载接口</summary>
    public async Task<(bool Success, string Message, Stream? Stream, long Received, long TotalSize)> DownloadChunkAsync(
        string fileId, long offset, long length)
    {
        try
        {
            var url = $"api/file/download/chunk/{Uri.EscapeDataString(fileId)}?offset={offset}&length={length}";
            var resp = await _api.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
                return (false, $"服务器返回 {resp.StatusCode}", null, 0, 0);

            // 解析 X-Total-Size / X-Actual-Size 自定义响应头
            long totalSize = 0;
            if (resp.Headers.TryGetValues("X-Total-Size", out var tValues)
                && long.TryParse(tValues.FirstOrDefault(), out var t))
                totalSize = t;

            long received = 0;
            if (resp.Headers.TryGetValues("X-Actual-Size", out var aValues)
                && long.TryParse(aValues.FirstOrDefault(), out var a))
                received = a;
            else if (resp.Content.Headers.ContentLength.HasValue)
                received = resp.Content.Headers.ContentLength.Value;

            var stream = await resp.Content.ReadAsStreamAsync();
            return (true, "OK", stream, received, totalSize);
        }
        catch (Exception ex)
        {
            return (false, $"网络错误: {ex.Message}", null, 0, 0);
        }
    }

    // ============================================================
    //  辅助：计算 MD5 + SHA1
    // ============================================================
    public static async Task<(string Md5, string Sha1, long Size)> ComputeFileHashAsync(string filePath,
        IProgress<(long Read, long Total)>? progress = null)
    {
        using var md5 = MD5.Create();
        using var sha1 = SHA1.Create();
        const int bufferSize = 1024 * 1024; // 1MB
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytes = new byte[bufferSize];
        int count;
        long totalRead = 0;
        var total = fs.Length;
        while ((count = await fs.ReadAsync(bytes)) > 0)
        {
            md5.TransformBlock(bytes, 0, count, bytes, 0);
            sha1.TransformBlock(bytes, 0, count, bytes, 0);
            totalRead += count;
            progress?.Report((totalRead, total));
        }
        md5.TransformFinalBlock([], 0, 0);
        sha1.TransformFinalBlock([], 0, 0);
        var md5Hex = BitConverter.ToString(md5.Hash!).Replace("-", "").ToLowerInvariant();
        var sha1Hex = BitConverter.ToString(sha1.Hash!).Replace("-", "").ToLowerInvariant();
        return (md5Hex, sha1Hex, total);
    }
}
