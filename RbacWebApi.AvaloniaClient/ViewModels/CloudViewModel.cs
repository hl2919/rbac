using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.DTOs;
using System.Collections.ObjectModel;
using System.IO;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>下载列表项：UI 绑定对象，进度与 SQLite 持久化双向同步</summary>
public partial class DownloadListItem : ObservableObject
{
    [ObservableProperty] private string _fileId;
    [ObservableProperty] private string _fileName;
    [ObservableProperty] private string _localPath;
    [ObservableProperty] private long _totalSize;
    [ObservableProperty] private long _downloadedSize;
    [ObservableProperty] private int _status;
    [ObservableProperty] private string _statusText;
    [ObservableProperty] private double _progress; // 0~1

    public DownloadListItem(DownloadRecord r)
    {
        _fileId = r.FileId;
        _fileName = r.FileName;
        _localPath = r.LocalPath;
        _totalSize = r.TotalSize;
        _downloadedSize = r.DownloadedSize;
        _status = r.Status;
        _statusText = DownloadStatus.ToText(r.Status);
        _progress = r.TotalSize > 0 ? (double)r.DownloadedSize / r.TotalSize : 0;
    }

    partial void OnDownloadedSizeChanged(long value)
    {
        Progress = TotalSize > 0 ? (double)value / TotalSize : 0;
    }

    partial void OnStatusChanged(int value)
    {
        StatusText = DownloadStatus.ToText(value);
    }

    public DownloadRecord ToRecord()
    {
        return new DownloadRecord
        {
            FileId = FileId,
            FileName = FileName,
            LocalPath = LocalPath,
            TotalSize = TotalSize,
            DownloadedSize = DownloadedSize,
            Status = Status,
            CreateTime = DateTimeOffset.Now,
            LastUpdateTime = DateTimeOffset.Now
        };
    }
}

/// <summary>云盘页：开通状态 + 文件列表 + 上传/下载 + 下载历史 + SQLite 持久化</summary>
public partial class CloudViewModel : ViewModelBase
{
    private readonly ICloudDiskService? _disk;
    private readonly IFileApiService? _files;
    private readonly IDownloadHistoryService? _downloadHistory;

    [ObservableProperty] private bool _activated;
    [ObservableProperty] private long _quota;
    [ObservableProperty] private long _usedSize;
    [ObservableProperty] private string _status = string.Empty;

    public long FreeSize => Quota - UsedSize;

    public ObservableCollection<UserFileInfoResponse> Items { get; } = [];
    /// <summary>下载列表：当前正在下载 + 已完成的历史记录</summary>
    public ObservableCollection<DownloadListItem> DownloadItems { get; } = [];

    private string? _currentFolderId;
    private readonly Stack<string?> _navStack = new();

    [ObservableProperty] private string _currentPath = "根目录";
    [ObservableProperty] private string _newFolderName = string.Empty;
    [ObservableProperty] private string _uploadStatus = string.Empty;
    [ObservableProperty] private UserFileInfoResponse? _selectedItem;

    [ObservableProperty] private double _uploadProgress;
    [ObservableProperty] private double _downloadProgress;

    /// <summary>选中的下载列表项（用于暂停/重试/打开等命令）</summary>
    [ObservableProperty] private DownloadListItem? _selectedDownloadItem;

    public IStorageProvider? StorageProvider { get; set; }

    public CloudViewModel()
    {
        DesignMode(() =>
        {
            Activated = true;
            Quota = 10L * 1024 * 1024 * 1024;
            UsedSize = 3L * 1024 * 1024 * 1024 + 256 * 1024 * 1024;
            Status = "云盘已开通（设计期示例）";
            OnPropertyChanged(nameof(FreeSize));

            var now = DateTimeOffset.Now;
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F01", FileName = "工作文档", IsFolder = true, FileSize = 0, FileExtension = null, UploadStatus = 2, CreateTime = now.AddDays(-20) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F02", FileName = "项目方案.docx", IsFolder = false, FileSize = 2L * 1024 * 1024, FileExtension = ".docx", UploadStatus = 2, CreateTime = now.AddDays(-3) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F03", FileName = "演示视频.mp4", IsFolder = false, FileSize = 128L * 1024 * 1024, FileExtension = ".mp4", UploadStatus = 2, CreateTime = now.AddHours(-2) });

            DownloadItems.Add(new DownloadListItem(new DownloadRecord
            {
                FileId = "01AN4Z07BY79KA1307SR9X4F03",
                FileName = "演示视频.mp4",
                LocalPath = @"D:\Downloads\演示视频.mp4",
                TotalSize = 128L * 1024 * 1024,
                DownloadedSize = 64L * 1024 * 1024,
                Status = DownloadStatus.Downloading,
                CreateTime = now.AddMinutes(-15),
                LastUpdateTime = now
            }));
            DownloadItems.Add(new DownloadListItem(new DownloadRecord
            {
                FileId = "01AN4Z07BY79KA1307SR9X4F02",
                FileName = "项目方案.docx",
                LocalPath = @"D:\Downloads\项目方案.docx",
                TotalSize = 2L * 1024 * 1024,
                DownloadedSize = 2L * 1024 * 1024,
                Status = DownloadStatus.Completed,
                CreateTime = now.AddDays(-1),
                LastUpdateTime = now.AddDays(-1)
            }));
            CurrentPath = "根目录（设计期示例）";
        });
    }

    public CloudViewModel(ICloudDiskService disk, IFileApiService files, IDownloadHistoryService downloadHistory) : this()
    {
        _disk = disk;
        _files = files;
        _downloadHistory = downloadHistory;
    }

    /// <summary>从 SQLite 加载历史下载列表（在 View Loaded 时调用）</summary>
    [RelayCommand]
    public async Task LoadDownloadHistoryAsync()
    {
        if (_downloadHistory == null) return;
        var records = await _downloadHistory.GetAllAsync();
        DownloadItems.Clear();
        foreach (var r in records)
        {
            // 已完成的不重新计算，已暂停的可手动重试
            DownloadItems.Add(new DownloadListItem(r));
        }
    }

    // ========== 云盘状态/列表刷新 ==========

    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (_disk == null) { Status = "服务未初始化"; return; }
        Status = "开通中...";
        var (ok, msg) = await _disk.ActivateAsync(10);
        Status = msg;
        if (ok) await RefreshStatusAsync();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        if (_disk == null) { Status = "服务未初始化"; return; }
        var (ok, msg, data) = await _disk.GetStatusAsync();
        if (ok && data != null)
        {
            Activated = data.Activated;
            Quota = data.Quota;
            UsedSize = data.UsedSize;
            Status = data.Activated ? "云盘已开通" : "未开通云盘";
            OnPropertyChanged(nameof(FreeSize));
            if (data.Activated) await RefreshListAsync();
        }
        else
        {
            Activated = false;
            Status = msg;
        }
    }

    [RelayCommand]
    public async Task RefreshListAsync()
    {
        if (_files == null) { Status = "服务未初始化"; return; }
        var (ok, msg, data) = await _files.GetUserFileListAsync(_currentFolderId);
        Items.Clear();
        if (ok && data != null)
        {
            foreach (var f in data.Items) Items.Add(f);
            CurrentPath = _currentFolderId == null ? "根目录" : $"文件夹 {_currentFolderId}";
            Status = $"共 {data.Total} 项";
        }
        else Status = msg;
    }

    // ========== 文件夹导航 ==========

    [RelayCommand]
    private async Task UpLevelAsync()
    {
        if (_navStack.TryPop(out var parent))
        {
            _currentFolderId = parent;
            await RefreshListAsync();
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (SelectedItem is { IsFolder: true })
        {
            _navStack.Push(_currentFolderId);
            _currentFolderId = SelectedItem.Id;
            await RefreshListAsync();
        }
    }

    [RelayCommand]
    private async Task CreateFolderAsync()
    {
        if (_files == null) { Status = "服务未初始化"; return; }
        if (string.IsNullOrWhiteSpace(NewFolderName)) return;
        var (ok, msg, _) = await _files.CreateFolderAsync(NewFolderName.Trim(), _currentFolderId);
        Status = msg;
        if (ok) { NewFolderName = string.Empty; await RefreshListAsync(); }
    }

    // ========== 上传：文件选择对话框 ==========

    [RelayCommand]
    private async Task PickFileAndUploadAsync()
    {
        if (StorageProvider == null) { Status = "无法访问文件系统（设计期）"; return; }
        if (_files == null) { Status = "服务未初始化"; return; }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要上传的文件（可多选）",
            AllowMultiple = true
        });
        if (files.Count == 0) { Status = "已取消选择"; return; }

        var paths = new List<string>(files.Count);
        foreach (var f in files)
        {
            var local = f.Path.LocalPath;
            if (!string.IsNullOrEmpty(local)) paths.Add(local);
        }
        if (paths.Count == 0) { Status = "无法获取本地路径"; return; }

        await UploadFilesAsync(paths.ToArray());
    }

    public async Task UploadFilesAsync(string[] filePaths)
    {
        if (_files == null) { UploadStatus = "服务未初始化"; return; }
        var successCount = 0;
        for (var idx = 0; idx < filePaths.Length; idx++)
        {
            var filePath = filePaths[idx];
            UploadStatus = $"正在处理 {Path.GetFileName(filePath)}（{idx + 1}/{filePaths.Length}）";
            var ok = await UploadSingleFileAsync(filePath);
            if (ok) successCount++;
        }
        UploadProgress = 0;
        UploadStatus = $"上传完成：成功 {successCount}/{filePaths.Length}";
        await RefreshListAsync();
    }

    private async Task<bool> UploadSingleFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) { UploadStatus = $"文件不存在: {filePath}"; return false; }
        UploadStatus = $"计算哈希: {Path.GetFileName(filePath)}";
        var (md5, sha1, size) = await FileApiService.ComputeFileHashAsync(filePath);
        UploadProgress = 0;

        UploadStatus = $"初始化上传: {Path.GetFileName(filePath)}";
        const long chunkSize = 5 * 1024 * 1024;
        var (ok, msg, init) = await _files!.UploadInitAsync(Path.GetFileName(filePath), size, md5, sha1, _currentFolderId, chunkSize);
        if (!ok || init == null) { UploadStatus = msg; return false; }

        if (init.IsInstant)
        {
            UploadStatus = $"秒传成功: {Path.GetFileName(filePath)}";
            UploadProgress = 1;
            return true;
        }

        var totalChunks = init.TotalChunks;
        const int readBufferSize = 512 * 1024;
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            readBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[chunkSize];
        var uploadedBytes = 0L;

        for (var i = 0; i < totalChunks; i++)
        {
            if (init.UploadedChunkIndexes?.Contains(i) == true)
            {
                uploadedBytes += chunkSize;
                UploadProgress = (double)uploadedBytes / size;
                continue;
            }
            long toRead = chunkSize;
            using var ms = new MemoryStream();
            while (toRead > 0)
            {
                var read = await fs.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, toRead));
                if (read == 0) break;
                await ms.WriteAsync(buffer.AsMemory(0, read));
                toRead -= read;
            }
            ms.Position = 0;
            UploadStatus = $"上传分片 {i + 1}/{totalChunks} - {Path.GetFileName(filePath)}";
            var (cOk, cMsg, _) = await _files.UploadChunkAsync(init.FileId, i, ms);
            if (!cOk) { UploadStatus = $"分片{i}失败: {cMsg}"; return false; }
            uploadedBytes += ms.Length;
            UploadProgress = (double)uploadedBytes / size;
        }

        UploadStatus = $"合并: {Path.GetFileName(filePath)}";
        var (dOk, dMsg, _) = await _files.UploadCompleteAsync(init.FileId);
        if (!dOk) { UploadStatus = dMsg; return false; }
        UploadProgress = 1;
        return true;
    }

    // ========== 下载：选择文件夹 + 分块写入本地 + SQLite 进度 ==========

    /// <summary>下载选中文件：选择本地文件夹 → 调用 download/chunk 分块拉取 → 写入磁盘 + SQLite 持久化</summary>
    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (_files == null) { Status = "服务未初始化"; return; }
        if (StorageProvider == null) { Status = "无法访问文件系统（设计期）"; return; }
        if (SelectedItem is not { IsFolder: false } file)
        {
            Status = "请先选中一个文件（不能是文件夹）";
            return;
        }

        // 1. 选择本地文件夹
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择下载保存目录"
        });
        if (folders.Count == 0) { Status = "已取消下载"; return; }
        var folderLocalPath = folders[0].Path.LocalPath;
        if (string.IsNullOrEmpty(folderLocalPath)) { Status = "无法解析目标文件夹路径"; return; }

        // 2. 构造本地文件路径
        var safeName = string.IsNullOrWhiteSpace(file.FileName) ? file.Id : file.FileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(c, '_');
        var localFile = Path.Combine(folderLocalPath, safeName);

        // 3. 探测总大小
        Status = "正在获取文件信息...";
        var (sizeOk, sizeMsg, totalSize) = await _files.GetDownloadSizeAsync(file.Id);
        if (!sizeOk || totalSize <= 0)
        {
            Status = $"获取文件大小失败: {sizeMsg}";
            return;
        }

        // 4. 创建下载列表项 + 写入 SQLite
        var existing = await _downloadHistory!.GetAsync(file.Id);
        DownloadListItem? item;
        if (existing != null && existing.TotalSize == totalSize)
        {
            // 续传：复用历史记录
            existing.LocalPath = localFile;
            existing.Status = DownloadStatus.Downloading;
            item = new DownloadListItem(existing);
            // 替换 UI 中的旧记录
            var oldIdx = -1;
            for (var i = 0; i < DownloadItems.Count; i++)
                if (DownloadItems[i].FileId == file.Id) { oldIdx = i; break; }
            if (oldIdx >= 0) DownloadItems[oldIdx] = item;
            else DownloadItems.Add(item);
            await _downloadHistory.UpsertAsync(item.ToRecord());
        }
        else
        {
            item = new DownloadListItem(new DownloadRecord
            {
                FileId = file.Id,
                FileName = safeName,
                LocalPath = localFile,
                TotalSize = totalSize,
                DownloadedSize = 0,
                Status = DownloadStatus.Downloading,
                CreateTime = DateTimeOffset.Now,
                LastUpdateTime = DateTimeOffset.Now
            });
            DownloadItems.Add(item);
            await _downloadHistory!.UpsertAsync(item.ToRecord());
        }

        // 5. 启动分块下载任务
        _ = Task.Run(() => DownloadLoopAsync(item));
        Status = $"开始下载 {safeName}（总 {totalSize:N0} 字节）";
    }

    /// <summary>下载循环：5MB 一块调用 download/chunk 接口，写入本地 + 更新进度</summary>
    private async Task DownloadLoopAsync(DownloadListItem item)
    {
        const long chunkSize = 5 * 1024 * 1024;
        const int writeBufferSize = 512 * 1024;

        var startPos = item.DownloadedSize;
        // 已完成直接结束
        if (startPos >= item.TotalSize)
        {
            item.Status = DownloadStatus.Completed;
            await _downloadHistory!.UpdateStatusAsync(item.FileId, DownloadStatus.Completed);
            return;
        }

        // 打开本地目标文件（OpenOrCreate 支持断点续传）
        using var dest = new FileStream(item.LocalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None,
            writeBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        dest.Position = startPos;

        var current = startPos;
        while (current < item.TotalSize)
        {
            // 暂停检查
            if (item.Status == DownloadStatus.Paused)
            {
                await _downloadHistory!.UpdateProgressAsync(item.FileId, current, DownloadStatus.Paused);
                return;
            }
            // 失败退出（外层可重试）
            if (item.Status == DownloadStatus.Failed) return;

            var (cOk, cMsg, stream, received, totalFromHeader) = await _files!.DownloadChunkAsync(item.FileId, current, chunkSize);
            if (!cOk || stream == null)
            {
                item.Status = DownloadStatus.Failed;
                await _downloadHistory!.UpdateProgressAsync(item.FileId, current, DownloadStatus.Failed);
                return;
            }

            // 如果接口返回了总大小且和本地不一致，以接口为准重置
            if (totalFromHeader > 0 && totalFromHeader != item.TotalSize)
            {
                item.TotalSize = totalFromHeader;
            }

            // 0 字节表示已到末尾
            if (received == 0)
            {
                await stream.DisposeAsync();
                break;
            }

            await using (stream)
            {
                var buf = new byte[80 * 1024];
                int read;
                while ((read = await stream.ReadAsync(buf)) > 0)
                {
                    await dest.WriteAsync(buf.AsMemory(0, read));
                    current += read;
                    item.DownloadedSize = current;
                }
            }

            // 每分片持久化一次进度
            await _downloadHistory!.UpdateProgressAsync(item.FileId, current, DownloadStatus.Downloading);
        }

        await dest.FlushAsync();
        item.DownloadedSize = item.TotalSize;
        item.Status = DownloadStatus.Completed;
        await _downloadHistory!.UpdateProgressAsync(item.FileId, item.TotalSize, DownloadStatus.Completed);
    }

    /// <summary>暂停下载</summary>
    [RelayCommand]
    private void PauseDownload()
    {
        if (SelectedDownloadItem is { Status: DownloadStatus.Downloading } item)
        {
            item.Status = DownloadStatus.Paused;
        }
    }

    /// <summary>继续/重试下载</summary>
    [RelayCommand]
    private async Task ResumeDownloadAsync()
    {
        if (SelectedDownloadItem is not { } item) return;
        if (item.Status is DownloadStatus.Completed) return;
        item.Status = DownloadStatus.Downloading;
        _ = Task.Run(() => DownloadLoopAsync(item));
    }

    /// <summary>从下载列表移除（同时清掉 SQLite 记录，不删本地文件）</summary>
    [RelayCommand]
    private async Task RemoveDownloadAsync()
    {
        if (SelectedDownloadItem is not { } item) return;
        if (item.Status == DownloadStatus.Downloading) item.Status = DownloadStatus.Paused;
        DownloadItems.Remove(item);
        if (_downloadHistory != null)
            await _downloadHistory.DeleteAsync(item.FileId);
    }

    /// <summary>清除已完成下载记录</summary>
    [RelayCommand]
    private async Task ClearCompletedDownloadsAsync()
    {
        var done = DownloadItems.Where(x => x.Status == DownloadStatus.Completed).ToList();
        foreach (var d in done)
        {
            DownloadItems.Remove(d);
            if (_downloadHistory != null)
                await _downloadHistory.DeleteAsync(d.FileId);
        }
    }

    // ========== 删除 ==========

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_files == null) { Status = "服务未初始化"; return; }
        if (SelectedItem == null) return;
        var (ok, msg) = await _files.DeleteAsync(SelectedItem.Id);
        Status = msg;
        if (ok) await RefreshListAsync();
    }
}
