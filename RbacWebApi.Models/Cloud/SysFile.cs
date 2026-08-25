using SqlSugar;

namespace RbacWebApi.Models.Cloud;

/// <summary>
/// 系统文件表：通过 MD5 + SHA1 + 文件大小判断文件唯一性，实现秒传与去重存储
/// </summary>
[SugarTable("sys_file")]
public class SysFile : BaseEntity
{
    /// <summary>原始文件名</summary>
    [SugarColumn(ColumnName = "file_name", Length = 500, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>文件 MD5 哈希值（用于唯一性校验）</summary>
    [SugarColumn(ColumnName = "md5", Length = 64, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string Md5 { get; set; } = string.Empty;

    /// <summary>文件 SHA1 哈希值（用于唯一性校验）</summary>
    [SugarColumn(ColumnName = "sha1", Length = 64, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string Sha1 { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    [SugarColumn(ColumnName = "file_size", IsNullable = false)]
    public long FileSize { get; set; }

    /// <summary>
    /// 存储路径（相对于基础存储目录）：按 ULID 每个字符分割一层文件夹
    /// 例如 ULID 01M01QAG... → 0/1/M/0/1/Q/A/G.../original_filename.ext
    /// </summary>
    [SugarColumn(ColumnName = "storage_path", Length = 1000, IsNullable = false, ColumnDataType = "NVARCHAR")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>文件扩展名（不含.）</summary>
    [SugarColumn(ColumnName = "file_extension", Length = 20, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? FileExtension { get; set; }

    /// <summary>MIME 类型</summary>
    [SugarColumn(ColumnName = "content_type", Length = 100, IsNullable = true, ColumnDataType = "NVARCHAR")]
    public string? ContentType { get; set; }

    /// <summary>引用计数（有多少用户文件引用此系统文件）</summary>
    [SugarColumn(ColumnName = "ref_count", IsNullable = false)]
    public int RefCount { get; set; } = 0;
}
