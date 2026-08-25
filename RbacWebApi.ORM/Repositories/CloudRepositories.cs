using RbacWebApi.Models.Cloud;
using RbacWebApi.ORM;

namespace RbacWebApi.Repositories;

// ============================================================
//  系统文件仓储
// ============================================================

/// <summary>
/// 系统文件仓储接口：继承泛型仓储基础能力
/// </summary>
public interface ISysFileRepository : IBaseRepository<SysFile>
{
    /// <summary>按 MD5+SHA1+文件大小查找系统文件（判断唯一性）</summary>
    Task<SysFile?> FindByHashAsync(string md5, string sha1, long fileSize);
}

public class SysFileRepository : BaseRepository<SysFile>, ISysFileRepository
{
    public SysFileRepository(IDbContext dbContext) : base(dbContext) { }

    public async Task<SysFile?> FindByHashAsync(string md5, string sha1, long fileSize)
    {
        return await Client.Queryable<SysFile>()
            .Where(f => f.Md5 == md5 && f.Sha1 == sha1 && f.FileSize == fileSize)
            .FirstAsync();
    }
}

// ============================================================
//  用户云盘开通记录仓储
// ============================================================

public interface IUserCloudDiskRepository : IBaseRepository<UserCloudDisk>
{
    /// <summary>按用户 ID 查询云盘开通记录</summary>
    Task<UserCloudDisk?> GetByUserIdAsync(string userId);
}

public class UserCloudDiskRepository : BaseRepository<UserCloudDisk>, IUserCloudDiskRepository
{
    public UserCloudDiskRepository(IDbContext dbContext) : base(dbContext) { }

    public async Task<UserCloudDisk?> GetByUserIdAsync(string userId)
    {
        return await Client.Queryable<UserCloudDisk>()
            .Where(d => d.UserId == userId)
            .FirstAsync();
    }
}
