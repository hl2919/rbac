using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using RbacWebApi.DTOs;
using RbacWebApi.Models.Cloud;
using RbacWebApi.ORM;
using RbacWebApi.Repositories;
using SqlSugar;

namespace RbacWebApi.Services.Cloud;

public class FileService : IFileService
{
    private readonly ISysFileRepository _sysFileRepo;
    private readonly IUserCloudDiskRepository _diskRepo;
    private readonly IDbContext _dbContext;
    private readonly ICloudDiskService _cloudDiskService;
    private readonly FileStorageSettings _settings;

    public FileService(
        ISysFileRepository sysFileRepo,
        IUserCloudDiskRepository diskRepo,
        IDbContext dbContext,
        ICloudDiskService cloudDiskService,
        IOptions<FileStorageSettings> settings)
    {
        _sysFileRepo = sysFileRepo;
        _diskRepo = diskRepo;
        _dbContext = dbContext;
        _cloudDiskService = cloudDiskService;
        _settings = settings.Value;
    }

    // ============================================================
    //  上传初始化：秒传 or 开启分片上传
    // ============================================================
    public async Task<(bool Success, string Message, UploadInitResponse? Response)> UploadInitAsync(
        string userId, UploadInitRequest request)
    {
        if (!await _cloudDiskService.IsActivatedAsync(userId))
            return (false, "请先开通云盘", null);

        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        // 0. 检查父文件夹存在性（若指定）
        if (!string.IsNullOrWhiteSpace(request.ParentFolderId))
        {
            var parent = await client.Queryable<UserFile>()
                .AS(tableName)
                .Where(f => f.Id == request.ParentFolderId && f.IsFolder)
                .FirstAsync();
            if (parent == null)
                return (false, "指定的父文件夹不存在", null);
        }

        // 1. 检查系统文件表是否存在相同文件（MD5+SHA1+Size 三重校验）
        var sysFile = await _sysFileRepo.FindByHashAsync(request.Md5, request.Sha1, request.FileSize);
        if (sysFile != null)
        {
            // 秒传：创建用户文件记录，引用已有系统文件
            var userFile = new UserFile
            {
                SysFileId = sysFile.Id,
                ParentFolderId = string.IsNullOrWhiteSpace(request.ParentFolderId) ? null : request.ParentFolderId,
                FileName = request.FileName,
                FileSize = request.FileSize,
                UploadStatus = 1,
                TotalChunks = 0,
                UploadedChunks = 0,
                ChunkSize = request.ChunkSize,
                Md5 = request.Md5,
                Sha1 = request.Sha1
            };
            await InsertUserFileAsync(tableName, userFile);

            // 系统文件引用计数 +1
            sysFile.RefCount++;
            await _sysFileRepo.UpdateAsync(sysFile);

            // 更新用户已用空间
            await UpdateUsedSizeAsync(userId, request.FileSize);

            return (true, "秒传成功", new UploadInitResponse
            {
                FileId = userFile.Id,
                IsInstant = true,
                TotalChunks = 0
            });
        }

        // 2. 未秒传：检查是否有未完成的上传（断点续传）
        var existing = await client.Queryable<UserFile>()
            .AS(tableName)
            .Where(f => f.Md5 == request.Md5 && f.Sha1 == request.Sha1 && f.UploadStatus == 0)
            .FirstAsync();

        if (existing != null)
        {
            // 断点续传：返回已上传分片
            var uploadedIndexes = GetUploadedChunkIndexes(existing.Id, existing.TotalChunks);
            return (true, "继续上传", new UploadInitResponse
            {
                FileId = existing.Id,
                IsInstant = false,
                TotalChunks = existing.TotalChunks,
                UploadedChunkIndexes = uploadedIndexes
            });
        }

        // 3. 新建上传记录
        var totalChunks = (int)Math.Ceiling((double)request.FileSize / request.ChunkSize);
        var newFile = new UserFile
        {
            ParentFolderId = string.IsNullOrWhiteSpace(request.ParentFolderId) ? null : request.ParentFolderId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            UploadStatus = 0,
            TotalChunks = totalChunks,
            UploadedChunks = 0,
            ChunkSize = request.ChunkSize,
            Md5 = request.Md5,
            Sha1 = request.Sha1
        };
        await InsertUserFileAsync(tableName, newFile);

        // 创建分片临时目录
        var chunkDir = GetChunkDir(newFile.Id);
        Directory.CreateDirectory(chunkDir);

        return (true, "开始上传", new UploadInitResponse
        {
            FileId = newFile.Id,
            IsInstant = false,
            TotalChunks = totalChunks
        });
    }

    // ============================================================
    //  分片上传
    // ============================================================
    public async Task<(bool Success, string Message, UploadChunkResponse? Response)> UploadChunkAsync(
        string userId, string fileId, int chunkIndex, Stream chunkStream)
    {
        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        var userFile = await client.Queryable<UserFile>()
            .AS(tableName)
            .Where(f => f.Id == fileId)
            .FirstAsync();

        if (userFile == null)
            return (false, "文件记录不存在", null);

        if (userFile.IsFolder)
            return (false, "文件夹不能上传分片", null);

        if (userFile.UploadStatus == 1)
            return (false, "文件已上传完成", null);

        if (chunkIndex < 0 || chunkIndex >= userFile.TotalChunks)
            return (false, $"分片索引超出范围(0-{userFile.TotalChunks - 1})", null);

        // 保存分片到临时目录
        var chunkPath = Path.Combine(GetChunkDir(fileId), $"chunk_{chunkIndex:D6}");
        await using (var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write))
        {
            await chunkStream.CopyToAsync(fs);
        }

        // 更新已上传分片数
        userFile.UploadedChunks = GetUploadedChunkIndexes(fileId, userFile.TotalChunks).Count;
        await client.Updateable(userFile).AS(tableName).ExecuteCommandAsync();

        return (true, "分片上传成功", new UploadChunkResponse
        {
            FileId = fileId,
            ChunkIndex = chunkIndex,
            UploadedChunks = userFile.UploadedChunks,
            TotalChunks = userFile.TotalChunks
        });
    }

    // ============================================================
    //  上传完成：合并分片 → 计算哈希 → 写系统文件表 → 移动到 ULID 目录
    // ============================================================
    public async Task<(bool Success, string Message, UploadCompleteResponse? Response)> UploadCompleteAsync(
        string userId, string fileId)
    {
        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        var userFile = await client.Queryable<UserFile>()
            .AS(tableName)
            .Where(f => f.Id == fileId)
            .FirstAsync();

        if (userFile == null)
            return (false, "文件记录不存在", null);

        if (userFile.IsFolder)
            return (false, "文件夹无需完成上传", null);

        if (userFile.UploadStatus == 1)
            return (false, "文件已上传完成，无需重复操作", null);

        var chunkDir = GetChunkDir(fileId);
        var mergedPath = Path.Combine(chunkDir, "merged.tmp");

        // 1. 按顺序合并所有分片
        await using (var mergedFs = new FileStream(mergedPath, FileMode.Create, FileAccess.Write))
        {
            for (var i = 0; i < userFile.TotalChunks; i++)
            {
                var chunkPath = Path.Combine(chunkDir, $"chunk_{i:D6}");
                if (!File.Exists(chunkPath))
                    return (false, $"分片 {i} 缺失，请重新上传该分片", null);
                await using var chunkFs = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                await chunkFs.CopyToAsync(mergedFs);
            }
        }

        // 2. 计算合并后文件的 MD5、SHA1、大小
        var (md5, sha1, fileSize) = await ComputeHashAndSizeAsync(mergedPath);

        // 3. 再次检查系统文件表（防止并发上传同一文件）
        var sysFile = await _sysFileRepo.FindByHashAsync(md5, sha1, fileSize);

        bool isInstant;
        if (sysFile != null)
        {
            // 系统已有此文件，直接引用，删除临时合并文件
            File.Delete(mergedPath);
            isInstant = true;
        }
        else
        {
            // 新系统文件：AOP 自动生成 ULID 主键
            sysFile = new SysFile
            {
                FileName = userFile.FileName,
                Md5 = md5,
                Sha1 = sha1,
                FileSize = fileSize,
                FileExtension = GetExtension(userFile.FileName),
                ContentType = GetContentType(userFile.FileName),
                RefCount = 0
            };
            await _sysFileRepo.InsertAsync(sysFile);

            // 按 ULID 每个字符分割一层文件夹，存放到最深层目录
            var storageRelPath = BuildUlidStoragePath(sysFile.Id, userFile.FileName);
            sysFile.StoragePath = storageRelPath;
            await _sysFileRepo.UpdateAsync(sysFile);

            // 移动物理文件
            var fullDestPath = GetFullPath(storageRelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath)!);
            File.Move(mergedPath, fullDestPath, overwrite: true);

            isInstant = false;
        }

        // 4. 更新用户文件记录
        userFile.SysFileId = sysFile.Id;
        userFile.UploadStatus = 1;
        userFile.UploadedChunks = userFile.TotalChunks;
        userFile.FileSize = fileSize;
        await client.Updateable(userFile).AS(tableName).ExecuteCommandAsync();

        // 5. 系统文件引用计数 +1
        sysFile.RefCount++;
        await _sysFileRepo.UpdateAsync(sysFile);

        // 6. 更新用户已用空间
        await UpdateUsedSizeAsync(userId, fileSize);

        // 7. 清理分片临时目录
        if (Directory.Exists(chunkDir))
            Directory.Delete(chunkDir, recursive: true);

        return (true, "上传完成", new UploadCompleteResponse
        {
            FileId = fileId,
            SysFileId = sysFile.Id,
            IsInstant = isInstant,
            Message = isInstant ? "文件秒传完成（系统已存在相同文件）" : "文件上传并存储完成"
        });
    }

    // ============================================================
    //  新建文件夹
    // ============================================================
    public async Task<(bool Success, string Message, string? FolderId)> CreateFolderAsync(
        string userId, CreateFolderRequest request)
    {
        if (!await _cloudDiskService.IsActivatedAsync(userId))
            return (false, "请先开通云盘", null);

        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        // 检查父文件夹存在性
        var parentId = string.IsNullOrWhiteSpace(request.ParentFolderId) ? null : request.ParentFolderId;
        if (parentId != null)
        {
            var parent = await client.Queryable<UserFile>()
                .AS(tableName)
                .Where(f => f.Id == parentId && f.IsFolder)
                .FirstAsync();
            if (parent == null)
                return (false, "指定的父文件夹不存在", null);
        }

        // 同级同名文件夹检测
        var siblingSameName = parentId == null
            ? await client.Queryable<UserFile>().AS(tableName)
                .Where(f => f.ParentFolderId == null && f.FileName == request.FolderName && f.IsFolder).AnyAsync()
            : await client.Queryable<UserFile>().AS(tableName)
                .Where(f => f.ParentFolderId == parentId && f.FileName == request.FolderName && f.IsFolder).AnyAsync();
        if (siblingSameName)
            return (false, "同级已存在同名文件夹", null);

        var folder = new UserFile
        {
            SysFileId = null,
            ParentFolderId = parentId,
            FileName = request.FolderName,
            IsFolder = true,
            FileSize = 0,
            UploadStatus = 1,
            TotalChunks = 0,
            UploadedChunks = 0,
            ChunkSize = 0,
            Md5 = null,
            Sha1 = null
        };
        await InsertUserFileAsync(tableName, folder);
        return (true, "文件夹创建成功", folder.Id);
    }

    // ============================================================
    //  用户文件/文件夹列表（分页）
    // ============================================================
    public async Task<PageResponse<UserFileInfoResponse>> GetUserFileListAsync(
        string userId, UserFileListRequest request)
    {
        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        var keyword = request.Keyword?.Trim();
        var parentId = string.IsNullOrWhiteSpace(request.ParentFolderId) ? null : request.ParentFolderId;
        var safePage = NormalizePage(request);

        // 构建查询条件
        var query = client.Queryable<UserFile>().AS(tableName);

        if (parentId == null)
            query = query.Where(f => f.ParentFolderId == null);
        else
            query = query.Where(f => f.ParentFolderId == parentId);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(f => f.FileName.Contains(keyword));

        // 文件夹排在前面，然后按创建时间降序
        query = query.OrderBy(f => f.IsFolder ? 0 : 1, OrderByType.Asc)
                     .OrderBy(f => f.CreateTime, OrderByType.Desc);

        var total = await query.CountAsync();
        var items = total > 0
            ? await query
                .Skip((safePage.PageIndex - 1) * safePage.PageSize)
                .Take(safePage.PageSize)
                .ToListAsync()
            : [];

        // 批量查询关联的系统文件（获取扩展名/Content-Type）
        var sysFileIds = items.Select(f => f.SysFileId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var sysFiles = sysFileIds.Count > 0
            ? (await client.Queryable<SysFile>()
                .Where(s => sysFileIds.Contains(s.Id))
                .ToListAsync()).ToDictionary(s => s.Id)
            : new Dictionary<string, SysFile>();

        var response = new PageResponse<UserFileInfoResponse>
        {
            PageIndex = safePage.PageIndex,
            PageSize = safePage.PageSize,
            Total = (int)total,
            Items = items.Select(f =>
            {
                sysFiles.TryGetValue(f.SysFileId ?? "", out var sf);
                return new UserFileInfoResponse
                {
                    Id = f.Id,
                    ParentFolderId = f.ParentFolderId,
                    FileName = f.FileName,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    UploadStatus = f.UploadStatus,
                    FileExtension = sf?.FileExtension,
                    ContentType = sf?.ContentType,
                    CreateTime = f.CreateTime,
                    LastUpdateTime = f.LastUpdateTime
                };
            }).ToList()
        };
        return response;
    }

    // ============================================================
    //  删除文件或文件夹（文件夹递归）
    // ============================================================
    public async Task<(bool Success, string Message)> DeleteFileAsync(string userId, string fileId)
    {
        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        var userFile = await client.Queryable<UserFile>()
            .AS(tableName)
            .Where(f => f.Id == fileId)
            .FirstAsync();
        if (userFile == null)
            return (false, "记录不存在");

        long sizeToDecrease = 0;
        var sysFileToDecref = new List<(string SysFileId, long Size)>();

        if (userFile.IsFolder)
        {
            // 递归收集所有后代 + 自身
            var descendants = await CollectDescendantsAsync(tableName, fileId);
            var allIds = new[] { fileId }.Concat(descendants.Select(d => d.Id)).ToList();

            foreach (var d in descendants.Where(x => !x.IsFolder && !string.IsNullOrEmpty(x.SysFileId)))
            {
                sysFileToDecref.Add((d.SysFileId!, d.FileSize));
                sizeToDecrease += d.FileSize;
            }

            await client.Deleteable<UserFile>().AS(tableName).Where(f => allIds.Contains(f.Id)).ExecuteCommandAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(userFile.SysFileId))
                sysFileToDecref.Add((userFile.SysFileId, userFile.FileSize));
            sizeToDecrease = userFile.FileSize;

            await client.Deleteable<UserFile>().AS(tableName).Where(f => f.Id == fileId).ExecuteCommandAsync();
        }

        // 处理 sys_file 引用计数
        foreach (var (sfId, size) in sysFileToDecref)
        {
            var sf = await _sysFileRepo.GetByIdAsync(sfId);
            if (sf == null) continue;
            sf.RefCount = Math.Max(0, sf.RefCount - 1);
            if (sf.RefCount <= 0)
            {
                var fullPath = GetFullPath(sf.StoragePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                await _sysFileRepo.DeleteByIdAsync(sf.Id);
            }
            else
            {
                await _sysFileRepo.UpdateAsync(sf);
            }
        }

        // 更新已用空间
        if (sizeToDecrease > 0)
            await UpdateUsedSizeAsync(userId, -sizeToDecrease);

        return (true, "删除成功");
    }

    // ============================================================
    //  获取下载文件信息
    // ============================================================
    public async Task<(SysFile? SysFile, UserFile? UserFile)> GetFileInfoForDownloadAsync(
        string userId, string fileId)
    {
        var tableName = _cloudDiskService.GetUserFileTableName(userId);
        var client = _dbContext.Client;

        var userFile = await client.Queryable<UserFile>()
            .AS(tableName)
            .Where(f => f.Id == fileId && !f.IsFolder && f.UploadStatus == 1)
            .FirstAsync();
        if (userFile == null || string.IsNullOrEmpty(userFile.SysFileId))
            return (null, null);

        var sysFile = await _sysFileRepo.GetByIdAsync(userFile.SysFileId);
        return (sysFile, userFile);
    }

    /// <summary>
    /// 分块下载：返回物理文件完整路径 + 总大小 + 实际可读取字节数。
    /// 服务层不打开流，由 Controller 通过 FileStream + Range 直接写出。
    /// </summary>
    public async Task<(bool Success, string Message, string? FullPath, long TotalSize, long ActualLength)> GetFileChunkAsync(
        string userId, string fileId, long offset, long length)
    {
        if (offset < 0) return (false, "offset 不能为负数", null, 0, 0);
        if (length <= 0) return (false, "length 必须大于 0", null, 0, 0);

        var (sysFile, userFile) = await GetFileInfoForDownloadAsync(userId, fileId);
        if (sysFile == null || userFile == null)
            return (false, "文件不存在或未上传完成", null, 0, 0);

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "files");
        var fullPath = Path.Combine(basePath, sysFile.StoragePath);
        if (!File.Exists(fullPath))
            return (false, "物理文件不存在", null, 0, 0);

        var totalSize = sysFile.FileSize;
        // 越过文件末尾：返回 0 字节但仍是成功（客户端据此结束循环）
        if (offset >= totalSize)
            return (true, "已到文件末尾", fullPath, totalSize, 0);

        var actual = Math.Min(length, totalSize - offset);
        return (true, "OK", fullPath, totalSize, actual);
    }

    // ============================================================
    //  辅助方法
    // ============================================================

    /// <summary>按 ULID 每个字符分割一层文件夹，最深层放源文件</summary>
    private static string BuildUlidStoragePath(string ulid, string fileName)
    {
        var parts = ulid.Select(c => c.ToString()).Append(fileName);
        return Path.Combine(parts.ToArray());
    }

    /// <summary>获取分片临时目录</summary>
    private string GetChunkDir(string fileId) => Path.Combine(_settings.BasePath, _settings.ChunkDir, fileId);

    /// <summary>获取文件完整物理路径</summary>
    private string GetFullPath(string relativePath) => Path.Combine(_settings.BasePath, relativePath);

    /// <summary>获取已上传分片索引列表</summary>
    private List<int> GetUploadedChunkIndexes(string fileId, int totalChunks)
    {
        var chunkDir = GetChunkDir(fileId);
        if (!Directory.Exists(chunkDir)) return [];

        var indexes = new List<int>();
        for (var i = 0; i < totalChunks; i++)
        {
            if (File.Exists(Path.Combine(chunkDir, $"chunk_{i:D6}")))
                indexes.Add(i);
        }
        return indexes;
    }

    /// <summary>插入用户文件记录（使用动态表名）</summary>
    private async Task InsertUserFileAsync(string tableName, UserFile file)
    {
        await _dbContext.Client.Insertable(file).AS(tableName).ExecuteCommandAsync();
    }

    /// <summary>更新用户已用空间</summary>
    private async Task UpdateUsedSizeAsync(string userId, long delta)
    {
        var disk = await _diskRepo.GetByUserIdAsync(userId);
        if (disk == null) return;
        disk.UsedSize = Math.Max(0, disk.UsedSize + delta);
        await _diskRepo.UpdateAsync(disk);
    }

    /// <summary>递归收集文件夹的所有后代记录（含子文件夹、子文件，不限层级）</summary>
    private async Task<List<UserFile>> CollectDescendantsAsync(string tableName, string folderId)
    {
        var client = _dbContext.Client;
        var result = new List<UserFile>();
        var queue = new Queue<string>();
        queue.Enqueue(folderId);
        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            var children = await client.Queryable<UserFile>()
                .AS(tableName)
                .Where(f => f.ParentFolderId == parent)
                .ToListAsync();
            foreach (var c in children)
            {
                result.Add(c);
                if (c.IsFolder) queue.Enqueue(c.Id);
            }
        }
        return result;
    }

    /// <summary>计算文件 MD5、SHA1 和大小</summary>
    private static async Task<(string Md5, string Sha1, long Size)> ComputeHashAndSizeAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var sha1 = SHA1.Create();
        await using var fs = File.OpenRead(filePath);
        var bytes = new byte[8192];
        int count;
        long totalSize = 0;
        while ((count = await fs.ReadAsync(bytes)) > 0)
        {
            md5.TransformBlock(bytes, 0, count, bytes, 0);
            sha1.TransformBlock(bytes, 0, count, bytes, 0);
            totalSize += count;
        }
        md5.TransformFinalBlock([], 0, 0);
        sha1.TransformFinalBlock([], 0, 0);

        var md5Hex = BitConverter.ToString(md5.Hash!).Replace("-", "").ToLowerInvariant();
        var sha1Hex = BitConverter.ToString(sha1.Hash!).Replace("-", "").ToLowerInvariant();
        return (md5Hex, sha1Hex, totalSize);
    }

    private static string? GetExtension(string fileName)
    {
        var idx = fileName.LastIndexOf('.');
        return idx >= 0 ? fileName[(idx + 1)..] : null;
    }

    private static string GetContentType(string fileName)
    {
        var ext = GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "pdf" => "application/pdf",
            "txt" => "text/plain",
            "mp4" => "video/mp4",
            "mp3" => "audio/mpeg",
            "zip" => "application/zip",
            "doc" or "docx" => "application/msword",
            "xls" or "xlsx" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }

    private static PageRequest NormalizePage(PageRequest page)
    {
        if (page.PageIndex <= 0) page.PageIndex = 1;
        if (page.PageSize <= 0) page.PageSize = 20;
        if (page.PageSize > 100) page.PageSize = 100;
        return page;
    }
}
