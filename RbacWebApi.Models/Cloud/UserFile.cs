using SqlSugar;

namespace RbacWebApi.Models.Cloud;

/// <summary>
/// 用户文件/文件夹表（模板）：开通云盘时按 user_file_{userId} 动态建表
/// 不使用 SugarTable 特性指定表名，运行时通过 AS() 动态指定
/// </summary>
public class UserFile : BaseEntity
{
    /// <summary>关联系统文件表 sys_file.Id（文件夹时为 null）</summary>
    [SugarColumn(ColumnName = "sys_file_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR")]
    public string? SysFileId { get; set; }

    /// <summary>父文件夹 ID，null 或空表示根目录</summary>
    [SugarColumn(ColumnName = "parent_folder_id", Length = 26, IsNullable = true, ColumnDataType = "VARCHAR")]
    public string? ParentFolderId { get; set; }

    /// <summary>文件名 / 文件夹名</summary>
    [SugarColumn(ColumnName = "file_name", Length = 500, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>是否为文件夹：1=文件夹，0=文件</summary>
    [SugarColumn(ColumnName = "is_folder", IsNullable = false)]
    public bool IsFolder { get; set; } = false;

    /// <summary>文件大小（字节），文件夹时为 0</summary>
    [SugarColumn(ColumnName = "file_size", IsNullable = false)]
    public long FileSize { get; set; }

    /// <summary>上传状态：0=上传中, 1=已完成，文件夹时默认为 1</summary>
    [SugarColumn(ColumnName = "upload_status", IsNullable = false)]
    public int UploadStatus { get; set; } = 0;

    /// <summary>总分片数，文件夹时为 0</summary>
    [SugarColumn(ColumnName = "total_chunks", IsNullable = false)]
    public int TotalChunks { get; set; } = 0;

    /// <summary>已上传分片数，文件夹时为 0</summary>
    [SugarColumn(ColumnName = "uploaded_chunks", IsNullable = false)]
    public int UploadedChunks { get; set; } = 0;

    /// <summary>分片大小（字节），文件夹时为 0</summary>
    [SugarColumn(ColumnName = "chunk_size", IsNullable = false)]
    public long ChunkSize { get; set; } = 0;

    /// <summary>文件 MD5（客户端提供，用于校验），文件夹时为 null</summary>
    [SugarColumn(ColumnName = "md5", Length = 64, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Md5 { get; set; }

    /// <summary>文件 SHA1（客户端提供，用于校验），文件夹时为 null</summary>
    [SugarColumn(ColumnName = "sha1", Length = 64, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? Sha1 { get; set; }
}
