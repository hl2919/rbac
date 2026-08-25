using RbacWebApi.DTOs;
using RbacWebApi.Models.Cloud;
using RbacWebApi.ORM;
using RbacWebApi.Repositories;

namespace RbacWebApi.Services.Cloud;

public class CloudDiskService : ICloudDiskService
{
    private readonly IUserCloudDiskRepository _diskRepo;
    private readonly IDbContext _dbContext;

    public CloudDiskService(IUserCloudDiskRepository diskRepo, IDbContext dbContext)
    {
        _diskRepo = diskRepo;
        _dbContext = dbContext;
    }

    public async Task<(bool Success, string Message)> ActivateAsync(string userId, ActivateCloudDiskRequest request)
    {
        // 幂等：已开通则直接更新配额
        var existing = await _diskRepo.GetByUserIdAsync(userId);
        if (existing != null)
        {
            existing.Quota = request.Quota;
            existing.Status = 1;
            await _diskRepo.UpdateAsync(existing);
            // 确保用户文件表存在
            EnsureUserFileTable(userId);
            return (true, "云盘已开通，配额已更新");
        }

        // 创建开通记录
        var disk = new UserCloudDisk
        {
            UserId = userId,
            Quota = request.Quota,
            UsedSize = 0,
            Status = 1
        };
        await _diskRepo.InsertAsync(disk);

        // 动态建用户文件表
        EnsureUserFileTable(userId);
        return (true, "云盘开通成功");
    }

    public async Task<CloudDiskStatusResponse?> GetStatusAsync(string userId)
    {
        var disk = await _diskRepo.GetByUserIdAsync(userId);
        if (disk == null) return null;
        return new CloudDiskStatusResponse
        {
            UserId = disk.UserId,
            Activated = true,
            Quota = disk.Quota,
            UsedSize = disk.UsedSize,
            Status = disk.Status
        };
    }

    public async Task<bool> IsActivatedAsync(string userId)
    {
        var disk = await _diskRepo.GetByUserIdAsync(userId);
        return disk != null && disk.Status == 1;
    }

    /// <summary>
    /// 用户文件表名规则：user_file_{userId}（userId 是 ULID，只含 0-9 A-Z，安全）
    /// </summary>
    public string GetUserFileTableName(string userId) => $"user_file_{userId}";

    /// <summary>
    /// 动态建表：用原生 SQL 按 UserFile 列结构创建表（幂等，IF NOT EXISTS）
    /// </summary>
    private void EnsureUserFileTable(string userId)
    {
        var tableName = GetUserFileTableName(userId);
        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    ""id""               VARCHAR(26)  NOT NULL PRIMARY KEY,
    ""sys_file_id""      VARCHAR(26),
    ""parent_folder_id"" VARCHAR(26),
    ""file_name""        NVARCHAR(500) NOT NULL,
    ""is_folder""        INTEGER       NOT NULL DEFAULT 0,
    ""file_size""        BIGINT        NOT NULL DEFAULT 0,
    ""upload_status""    INTEGER       NOT NULL DEFAULT 0,
    ""total_chunks""     INTEGER       NOT NULL DEFAULT 0,
    ""uploaded_chunks""  INTEGER       NOT NULL DEFAULT 0,
    ""chunk_size""       BIGINT        NOT NULL DEFAULT 0,
    ""md5""              NVARCHAR(64),
    ""sha1""             NVARCHAR(64),
    ""create_time""      TEXT          NOT NULL,
    ""last_update_time"" TEXT
);";
        _dbContext.Client.Ado.ExecuteCommand(sql);
    }
}
