using System;
using System.Collections.Generic;

namespace RbacWebApi.AvaloniaClient.Models;

/// <summary>下载记录：用于 SQLite 持久化，断点续传 + 下载历史</summary>
public class DownloadRecord
{
    /// <summary>主键：服务端用户文件 ID（一个用户文件对应一条下载记录）</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>本地文件名（保存到本地的名称）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>本地保存绝对路径</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>文件总大小（字节）</summary>
    public long TotalSize { get; set; }

    /// <summary>已下载字节数（用于断点续传）</summary>
    public long DownloadedSize { get; set; }

    /// <summary>下载状态：0=待下载, 1=下载中, 2=已暂停, 3=已完成, 4=失败</summary>
    public int Status { get; set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; set; }

    /// <summary>最后更新时间</summary>
    public DateTimeOffset? LastUpdateTime { get; set; }
}

/// <summary>下载状态枚举（与 DownloadRecord.Status 字段对应）</summary>
public static class DownloadStatus
{
    public const int Pending = 0;
    public const int Downloading = 1;
    public const int Paused = 2;
    public const int Completed = 3;
    public const int Failed = 4;

    public static string ToText(int status) => status switch
    {
        Pending => "待下载",
        Downloading => "下载中",
        Paused => "已暂停",
        Completed => "已完成",
        Failed => "失败",
        _ => "未知"
    };
}
