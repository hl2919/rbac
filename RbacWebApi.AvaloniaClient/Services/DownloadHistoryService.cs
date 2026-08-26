using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using RbacWebApi.AvaloniaClient.Models;

namespace RbacWebApi.AvaloniaClient.Services;

/// <summary>
/// 下载历史 SQLite 服务：管理 DownloadRecord 的 CRUD + 进度持久化。
/// 数据库文件位置：%AppData%/RbacWebApiClient/downloads.db
/// </summary>
public interface IDownloadHistoryService
{
    /// <summary>初始化数据库和表（幂等，启动时调用一次）</summary>
    Task InitializeAsync();

    /// <summary>插入或更新下载记录（按 FileId 主键）</summary>
    Task UpsertAsync(DownloadRecord record);

    /// <summary>根据 FileId 获取下载记录（包含已下载字节数，用于断点续传）</summary>
    Task<DownloadRecord?> GetAsync(string fileId);

    /// <summary>更新已下载字节数和状态</summary>
    Task UpdateProgressAsync(string fileId, long downloadedSize, int status);

    /// <summary>更新状态</summary>
    Task UpdateStatusAsync(string fileId, int status);

    /// <summary>删除下载记录</summary>
    Task DeleteAsync(string fileId);

    /// <summary>查询全部下载记录（按创建时间倒序）</summary>
    Task<List<DownloadRecord>> GetAllAsync();
}

public class DownloadHistoryService : IDownloadHistoryService
{
    private readonly string _connStr;

    public DownloadHistoryService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RbacWebApiClient");
        Directory.CreateDirectory(dir);
        _connStr = $"Data Source={Path.Combine(dir, "downloads.db")}";
    }

    public async Task InitializeAsync()
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS download_record (
                file_id          TEXT PRIMARY KEY,
                file_name        TEXT NOT NULL,
                local_path       TEXT NOT NULL,
                total_size       INTEGER NOT NULL,
                downloaded_size  INTEGER NOT NULL,
                status           INTEGER NOT NULL,
                create_time      TEXT NOT NULL,
                last_update_time TEXT
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertAsync(DownloadRecord record)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO download_record
                (file_id, file_name, local_path, total_size, downloaded_size, status, create_time, last_update_time)
            VALUES
                (@fileId, @fileName, @localPath, @totalSize, @downloadedSize, @status, @createTime, @lastUpdateTime)
            ON CONFLICT(file_id) DO UPDATE SET
                file_name = @fileName,
                local_path = @localPath,
                total_size = @totalSize,
                downloaded_size = @downloadedSize,
                status = @status,
                last_update_time = @lastUpdateTime;
            """;
        cmd.Parameters.AddWithValue("@fileId", record.FileId);
        cmd.Parameters.AddWithValue("@fileName", record.FileName);
        cmd.Parameters.AddWithValue("@localPath", record.LocalPath);
        cmd.Parameters.AddWithValue("@totalSize", record.TotalSize);
        cmd.Parameters.AddWithValue("@downloadedSize", record.DownloadedSize);
        cmd.Parameters.AddWithValue("@status", record.Status);
        cmd.Parameters.AddWithValue("@createTime", record.CreateTime.ToString("O"));
        cmd.Parameters.AddWithValue("@lastUpdateTime",
            record.LastUpdateTime?.ToString("O") ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DownloadRecord?> GetAsync(string fileId)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT file_id, file_name, local_path, total_size, downloaded_size, status, create_time, last_update_time FROM download_record WHERE file_id = @fileId";
        cmd.Parameters.AddWithValue("@fileId", fileId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapReader(reader);
    }

    public async Task UpdateProgressAsync(string fileId, long downloadedSize, int status)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE download_record SET downloaded_size = @size, status = @status, last_update_time = @now WHERE file_id = @fileId";
        cmd.Parameters.AddWithValue("@fileId", fileId);
        cmd.Parameters.AddWithValue("@size", downloadedSize);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.Now.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateStatusAsync(string fileId, int status)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE download_record SET status = @status, last_update_time = @now WHERE file_id = @fileId";
        cmd.Parameters.AddWithValue("@fileId", fileId);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.Now.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string fileId)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM download_record WHERE file_id = @fileId";
        cmd.Parameters.AddWithValue("@fileId", fileId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<DownloadRecord>> GetAllAsync()
    {
        var list = new List<DownloadRecord>();
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT file_id, file_name, local_path, total_size, downloaded_size, status, create_time, last_update_time FROM download_record ORDER BY create_time DESC";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapReader(reader));
        return list;
    }

    private static DownloadRecord MapReader(SqliteDataReader reader)
    {
        var lastUpdate = reader.IsDBNull(7)
            ? (DateTimeOffset?)null
            : DateTimeOffset.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind);
        return new DownloadRecord
        {
            FileId = reader.GetString(0),
            FileName = reader.GetString(1),
            LocalPath = reader.GetString(2),
            TotalSize = reader.GetInt64(3),
            DownloadedSize = reader.GetInt64(4),
            Status = reader.GetInt32(5),
            CreateTime = DateTimeOffset.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
            LastUpdateTime = lastUpdate
        };
    }
}
