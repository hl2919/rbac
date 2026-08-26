using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class CloudView : UserControl
{
    public CloudView()
    {
        InitializeComponent();
        // 拖拽事件注册（设计期 + 运行期都安全；AllowDrop 已在 XAML 设置）
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    public CloudView(CloudViewModel vm) : this()
    {
        DataContext = vm;
        // 注入 StorageProvider：View 已附加到视觉树后才能拿到 TopLevel
        Loaded += async (_, _) =>
        {
            if (vm.StorageProvider == null)
                vm.StorageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            // 加载历史下载列表
            await vm.LoadDownloadHistoryAsync();
        };
    }

    /// <summary>拖拽进入：标记为"复制"效果</summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    /// <summary>拖拽释放：提取文件路径并调用 VM.UploadFilesAsync</summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not CloudViewModel vm) return;
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files == null) return;

        // 仅保留真实文件（跳过目录）
        var paths = new List<string>();
        foreach (var f in files)
        {
            var local = f.Path.LocalPath;
            if (!string.IsNullOrEmpty(local) && File.Exists(local))
                paths.Add(local);
        }
        if (paths.Count == 0) return;

        e.DragEffects = DragDropEffects.Copy;
        await vm.UploadFilesAsync(paths.ToArray());
    }
}
