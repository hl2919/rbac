using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.DTOs;
using System.Collections.ObjectModel;
using System.IO;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>云盘页：开通状态 + 文件列表 + 上传/下载/建文件夹</summary>
public partial class CloudViewModel : ViewModelBase
{
    private readonly ICloudDiskService? _disk;
    private readonly IFileApiService? _files;

    [ObservableProperty] private bool _activated;
    [ObservableProperty] private long _quota;
    [ObservableProperty] private long _usedSize;
    [ObservableProperty] private string _status = string.Empty;

    public long FreeSize => Quota - UsedSize;

    public ObservableCollection<UserFileInfoResponse> Items { get; } = [];
    private string? _currentFolderId;
    private readonly Stack<string?> _navStack = new();

    [ObservableProperty] private string _currentPath = "根目录";
    [ObservableProperty] private string _newFolderName = string.Empty;
    [ObservableProperty] private string _uploadStatus = string.Empty;
    [ObservableProperty] private UserFileInfoResponse? _selectedItem;

    /// <summary>上传进度（0~1），供 UI ProgressBar 使用</summary>
    [ObservableProperty] private double _uploadProgress;
    /// <summary>下载进度（0~1），供 UI ProgressBar 使用</summary>
    [ObservableProperty] private double _downloadProgress;

    /// <summary>
    /// 文件系统访问入口：由 View 在构造时通过 TopLevel.GetTopLevel(this).StorageProvider 注入。
    /// 设计期为 null，命令中需先判空。
    /// </summary>
    public IStorageProvider? StorageProvider { get; set; }

    /// <summary>设计期无参构造：填充云盘状态和示例文件列表</summary>
    public CloudViewModel()
    {
        DesignMode(() =>
        {
            Activated = true;
            Quota = 10L * 1024 * 1024 * 1024;  // 10 GB
            UsedSize = 3L * 1024 * 1024 * 1024 + 256 * 1024 * 1024; // 3.25 GB
            Status = "云盘已开通（设计期示例）";
            OnPropertyChanged(nameof(FreeSize));

            var now = DateTimeOffset.Now;
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F01", FileName = "工作文档", IsFolder = true, FileSize = 0, FileExtension = null, UploadStatus = 2, CreateTime = now.AddDays(-20) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F02", FileName = "图片素材", IsFolder = true, FileSize = 0, FileExtension = null, UploadStatus = 2, CreateTime = now.AddDays(-15) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F03", FileName = "视频备份", IsFolder = true, FileSize = 0, FileExtension = null, UploadStatus = 2, CreateTime = now.AddDays(-10) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F04", FileName = "项目方案.docx", IsFolder = false, FileSize = 2L * 1024 * 1024, FileExtension = ".docx", UploadStatus = 2, CreateTime = now.AddDays(-3) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F05", FileName = "数据报表.xlsx", IsFolder = false, FileSize = 850L * 1024, FileExtension = ".xlsx", UploadStatus = 2, CreateTime = now.AddDays(-2) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F06", FileName = "产品原型.png", IsFolder = false, FileSize = 4L * 1024 * 1024, FileExtension = ".png", UploadStatus = 2, CreateTime = now.AddHours(-6) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F07", FileName = "演示视频.mp4", IsFolder = false, FileSize = 128L * 1024 * 1024, FileExtension = ".mp4", UploadStatus = 2, CreateTime = now.AddHours(-2) });
            Items.Add(new UserFileInfoResponse { Id = "01AN4Z07BY79KA1307SR9X4F08", FileName = "logo.psd", IsFolder = false, FileSize = 56L * 1024 * 1024, FileExtension = ".psd", UploadStatus = 1, CreateTime = now.AddMinutes(-15) });
            CurrentPath = "根目录（设计期示例）";
            UploadStatus = string.Empty;
        });
    }

    /// <summary>IoC 注入构造</summary>
    public CloudViewModel(ICloudDiskService disk, IFileApiService files) : this()
    {
        _disk = disk;
        _files = files;
    }

    // ========== 状态/列表刷新 ==========

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
        else
        {
            Status = msg;
        }
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
        if (ok)
        {
            NewFolderName = string.Empty;
            await RefreshListAsync();
        }
    }

    // ========== 上传：文件选择对话框 ==========

    /// <summary>打开文件选择对话框，选择文件后立即上传</summary>
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

    /// <summary>批量上传文件（供拖拽上传和文件选择对话框共同使用）</summary>
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

    /// <summary>上传单个本地文件：计算哈希 → 初始化 → 分片上传 → 合并</summary>
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

        // 分片上传：按 [已上传] 跳过已存在的分片
        var totalChunks = init.TotalChunks;
        const int readBufferSize = 512 * 1024; // 512KB 内部读缓冲区
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

    // ========== 下载：选择文件夹 + 分块写入本地 ==========

    /// <summary>下载选中文件：选择本地文件夹 → 分块拉取 → 写入磁盘</summary>
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
        if (string.IsNullOrEmpty(folderLocalPath))
        {
            Status = "无法解析目标文件夹路径";
            return;
        }

        // 2. 探测文件大小
        Status = "正在获取文件信息...";
        var (sizeOk, sizeMsg, totalSize) = await _files.GetDownloadSizeAsync(file.Id);
        if (!sizeOk || totalSize <= 0)
        {
            Status = $"获取文件大小失败: {sizeMsg}";
            return;
        }

        // 3. 打开本地目标文件（覆盖写）
        var safeName = string.IsNullOrWhiteSpace(file.FileName) ? file.Id : file.FileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(c, '_');
        var localFile = Path.Combine(folderLocalPath, safeName);

        // 断点续传：若本地已有部分，可从已下载字节继续
        long startPos = 0;
        if (File.Exists(localFile))
        {
            var existingLen = new FileInfo(localFile).Length;
            if (existingLen > 0 && existingLen < totalSize)
                startPos = existingLen;
            else if (existingLen == totalSize)
            {
                Status = $"文件已存在: {safeName}";
                DownloadProgress = 1;
                return;
            }
        }

        Status = $"开始下载 {safeName}（总 {totalSize:N0} 字节，从 {startPos:N0} 续传）";
        DownloadProgress = (double)startPos / totalSize;

        const long chunkSize = 5 * 1024 * 1024; // 5MB
        const int writeBufferSize = 512 * 1024;  // 512KB 内部写缓冲区
        using var dest = new FileStream(localFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None,
            writeBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        dest.Position = startPos;

        long current = startPos;
        while (current < totalSize)
        {
            var end = Math.Min(current + chunkSize - 1, totalSize - 1);
            var (cOk, cMsg, stream, received) = await _files.DownloadRangeAsync(file.Id, current, end);
            if (!cOk || stream == null)
            {
                Status = $"下载失败 @ {current}: {cMsg}";
                return;
            }

            await using (stream)
            {
                var buf = new byte[80 * 1024];
                int read;
                while ((read = await stream.ReadAsync(buf)) > 0)
                {
                    await dest.WriteAsync(buf.AsMemory(0, read));
                    current += read;
                    DownloadProgress = (double)current / totalSize;
                }
            }

            // 防御：若本次返回字节为 0 但又未到末尾，跳出避免死循环
            if (received == 0 && current < totalSize) break;
        }

        await dest.FlushAsync();
        DownloadProgress = 1;
        Status = current >= totalSize
            ? $"下载完成: {safeName}（{totalSize:N0} 字节）"
            : $"下载中断 @ {current}/{totalSize}，下次可继续";
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
