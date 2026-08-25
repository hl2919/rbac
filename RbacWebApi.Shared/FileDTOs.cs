using System.ComponentModel.DataAnnotations;

namespace RbacWebApi.DTOs;

// ============================================================
//  云盘开通
// ============================================================

/// <summary>开通云盘请求</summary>
public class ActivateCloudDiskRequest
{
    /// <summary>存储配额（字节），默认 10GB</summary>
    public long Quota { get; set; } = 10L * 1024 * 1024 * 1024;
}

/// <summary>云盘状态响应</summary>
public class CloudDiskStatusResponse
{
    public string UserId { get; set; } = string.Empty;
    public bool Activated { get; set; }
    public long Quota { get; set; }
    public long UsedSize { get; set; }
    public long FreeSize => Quota - UsedSize;
    public int Status { get; set; }
}

// ============================================================
//  文件上传
// ============================================================

/// <summary>上传初始化请求：客户端提交文件元信息，服务端判断是否秒传或开启分片上传</summary>
public class UploadInitRequest
{
    [Required(ErrorMessage = "文件名不能为空")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "文件大小不能为空")]
    public long FileSize { get; set; }

    [Required(ErrorMessage = "MD5不能为空")]
    public string Md5 { get; set; } = string.Empty;

    [Required(ErrorMessage = "SHA1不能为空")]
    public string Sha1 { get; set; } = string.Empty;

    /// <summary>所属父文件夹 ID，null 或空表示根目录</summary>
    public string? ParentFolderId { get; set; }

    /// <summary>分片大小（字节），默认 5MB</summary>
    public long ChunkSize { get; set; } = 5L * 1024 * 1024;
}

/// <summary>上传初始化响应</summary>
public class UploadInitResponse
{
    /// <summary>用户文件记录 ID</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>是否秒传（系统已存在相同文件）</summary>
    public bool IsInstant { get; set; }

    /// <summary>总分片数</summary>
    public int TotalChunks { get; set; }

    /// <summary>已上传分片数（断点续传时返回已上传的分片索引列表）</summary>
    public List<int> UploadedChunkIndexes { get; set; } = [];
}

/// <summary>分片上传响应</summary>
public class UploadChunkResponse
{
    public string FileId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int UploadedChunks { get; set; }
    public int TotalChunks { get; set; }
}

/// <summary>上传完成响应</summary>
public class UploadCompleteResponse
{
    public string FileId { get; set; } = string.Empty;
    public string SysFileId { get; set; } = string.Empty;
    public bool IsInstant { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ============================================================
//  文件列表与下载
// ============================================================

/// <summary>用户文件列表查询请求</summary>
public class UserFileListRequest : PageKeyRequest
{
    /// <summary>所属父文件夹 ID，null 或空表示根目录</summary>
    public string? ParentFolderId { get; set; }
}

/// <summary>用户文件信息响应</summary>
public class UserFileInfoResponse
{
    public string Id { get; set; } = string.Empty;

    /// <summary>父文件夹 ID，null 表示根目录</summary>
    public string? ParentFolderId { get; set; }

    /// <summary>文件名或文件夹名</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>是否为文件夹</summary>
    public bool IsFolder { get; set; }

    /// <summary>文件大小（文件夹时为 0）</summary>
    public long FileSize { get; set; }

    public int UploadStatus { get; set; }
    public string? FileExtension { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset? LastUpdateTime { get; set; }
}

// ============================================================
//  文件夹管理
// ============================================================

/// <summary>新建文件夹请求</summary>
public class CreateFolderRequest
{
    [Required(ErrorMessage = "文件夹名不能为空")]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>父文件夹 ID，null 或空表示根目录</summary>
    public string? ParentFolderId { get; set; }
}
