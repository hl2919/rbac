using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Services;
using RbacWebApi.Services.Cloud;

namespace RbacWebApi.Controllers;

/// <summary>
/// 文件管理控制器：上传（秒传/分片/断点续传）、下载（断点续传）、列表、删除
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IJwtTokenService _jwtTokenService;

    public FileController(IFileService fileService, IJwtTokenService jwtTokenService)
    {
        _fileService = fileService;
        _jwtTokenService = jwtTokenService;
    }

    private string? GetUserId() => _jwtTokenService.GetUserIdFromClaims(User);

    /// <summary>上传初始化：判断秒传或开启分片上传</summary>
    [HttpPost("upload/init")]
    public async Task<ActionResult<ApiResponse<UploadInitResponse>>> UploadInit([FromBody] UploadInitRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<UploadInitResponse>.Unauthorized());

        var result = await _fileService.UploadInitAsync(userId, request);
        if (!result.Success) return Ok(ApiResponse<UploadInitResponse>.Fail(result.Message));
        return Ok(ApiResponse<UploadInitResponse>.Success(result.Response, result.Message));
    }

    /// <summary>分片上传：fileId=用户文件ID, chunkIndex=分片索引(从0开始)</summary>
    [HttpPost("upload/chunk")]
    public async Task<ActionResult<ApiResponse<UploadChunkResponse>>> UploadChunk(
        [FromForm] string fileId,
        [FromForm] int chunkIndex,
        IFormFile file)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<UploadChunkResponse>.Unauthorized());

        if (file == null || file.Length == 0)
            return Ok(ApiResponse<UploadChunkResponse>.Fail("分片数据不能为空"));

        await using var stream = file.OpenReadStream();
        var result = await _fileService.UploadChunkAsync(userId, fileId, chunkIndex, stream);
        if (!result.Success) return Ok(ApiResponse<UploadChunkResponse>.Fail(result.Message));
        return Ok(ApiResponse<UploadChunkResponse>.Success(result.Response, result.Message));
    }

    /// <summary>上传完成：合并分片、写入系统文件表、移动到 ULID 目录</summary>
    [HttpPost("upload/complete")]
    public async Task<ActionResult<ApiResponse<UploadCompleteResponse>>> UploadComplete([FromBody] UploadCompleteRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<UploadCompleteResponse>.Unauthorized());

        var result = await _fileService.UploadCompleteAsync(userId, request.FileId);
        if (!result.Success) return Ok(ApiResponse<UploadCompleteResponse>.Fail(result.Message));
        return Ok(ApiResponse<UploadCompleteResponse>.Success(result.Response, result.Message));
    }

    /// <summary>分页查询用户文件列表</summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PageResponse<UserFileInfoResponse>>>> GetList([FromQuery] UserFileListRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<PageResponse<UserFileInfoResponse>>.Unauthorized());

        var page = await _fileService.GetUserFileListAsync(userId, request);
        return Ok(ApiResponse<PageResponse<UserFileInfoResponse>>.Success(page));
    }

    /// <summary>下载文件（支持 HTTP Range 断点续传）</summary>
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(string id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (sysFile, userFile) = await _fileService.GetFileInfoForDownloadAsync(userId, id);
        if (sysFile == null || userFile == null)
            return Ok(ApiResponse<object>.Fail("文件不存在或未上传完成", 404));

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "files");
        var fullPath = Path.Combine(basePath, sysFile.StoragePath);
        if (!System.IO.File.Exists(fullPath))
            return Ok(ApiResponse<object>.Fail("物理文件不存在", 404));

        // PhysicalFile 启用 Range 处理：客户端可发 Range 头实现断点续传
        var contentType = sysFile.ContentType ?? "application/octet-stream";
        return PhysicalFile(fullPath, contentType, userFile.FileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// 分块下载接口：按 offset + length 读取指定字节区间的数据流。
    /// 客户端循环调用此接口实现断点续传，进度可记录到本地 SQLite。
    /// </summary>
    /// <param name="id">用户文件 ID</param>
    /// <param name="offset">起始字节偏移（从 0 开始）</param>
    /// <param name="length">读取字节数（默认 5MB）</param>
    /// <returns>字节流（application/octet-stream）；响应头 X-Total-Size 表示文件总大小</returns>
    [HttpGet("download/chunk/{id}")]
    public async Task<IActionResult> DownloadChunk(string id, [FromQuery] long offset = 0, [FromQuery] long length = 5L * 1024 * 1024)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (ok, msg, fullPath, totalSize, actualLen) = await _fileService.GetFileChunkAsync(userId, id, offset, length);
        if (!ok || fullPath == null)
            return Ok(ApiResponse<object>.Fail(msg, 404));

        // 已到末尾：返回空流但带上 X-Total-Size / X-Actual-Size 头，客户端据此结束
        if (actualLen == 0)
        {
            Response.Headers["X-Total-Size"] = totalSize.ToString();
            Response.Headers["X-Actual-Size"] = "0";
            return new EmptyResult();
        }

        // 用 FileStream + Range 写入响应体；FileOptions.Asynchronous 提升吞吐
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            80 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        stream.Position = offset;

        Response.Headers["X-Total-Size"] = totalSize.ToString();
        Response.Headers["X-Actual-Size"] = actualLen.ToString();
        Response.Headers["Content-Length"] = actualLen.ToString();
        Response.ContentType = "application/octet-stream";
        return new FileStreamResult(stream, "application/octet-stream")
        {
            EnableRangeProcessing = false
        };
    }

    /// <summary>新建文件夹</summary>
    [HttpPost("folder")]
    public async Task<ActionResult<ApiResponse<string>>> CreateFolder([FromBody] CreateFolderRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<string>.Unauthorized());

        var result = await _fileService.CreateFolderAsync(userId, request);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.FolderId!, result.Message));
    }

    /// <summary>删除文件或文件夹（文件夹递归删除）</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(string id)
    {
        var userId = GetUserId();
        if (userId == null) return Ok(ApiResponse<string>.Unauthorized());

        var result = await _fileService.DeleteFileAsync(userId, id);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }
}

/// <summary>上传完成请求体</summary>
public class UploadCompleteRequest
{
    public string FileId { get; set; } = string.Empty;
}
