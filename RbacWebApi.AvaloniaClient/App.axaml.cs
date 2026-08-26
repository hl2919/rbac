using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.AvaloniaClient.ViewModels;
using RbacWebApi.AvaloniaClient.Views;

namespace RbacWebApi.AvaloniaClient;

public partial class App : Application
{
    private IServiceProvider _services = null!;
    private IAuthService _auth = null!;
    private LoginWindow? _loginWindow;
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = Program.Services;
        _auth = _services.GetRequiredService<IAuthService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 尝试恢复已保存的会话
            _auth.StateChanged += OnAuthStateChanged;

            // 初始化本地 SQLite 下载记录表（幂等，失败不影响启动）
            _ = _services.GetRequiredService<IDownloadHistoryService>().InitializeAsync();

            if (_auth.HasSavedSession())
            {
                // 有保存的会话：先显示主窗口，后台验证
                ShowMainWindow();
                _ = TryRestoreSessionAsync();
            }
            else
            {
                ShowLoginWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>尝试恢复会话</summary>
    private async Task TryRestoreSessionAsync()
    {
        var (ok, _) = await _auth.LoadSavedSessionAsync();
        if (!ok)
        {
            // 会话恢复失败，切回登录窗口
            ShowLoginWindow();
        }
    }

    /// <summary>认证状态变化时切换窗口</summary>
    private void OnAuthStateChanged()
    {
        if (_auth.Current != null)
        {
            // 已登录：显示主窗口
            ShowMainWindow();
        }
        else
        {
            // 已登出：显示登录窗口
            ShowLoginWindow();
        }
    }

    private void ShowLoginWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        if (_loginWindow == null)
        {
            // 通过 IoC 获取 LoginWindow（构造中注入 LoginViewModel）
            var loginVm = _services.GetRequiredService<LoginViewModel>();
            _loginWindow = new LoginWindow(loginVm);
            _loginWindow.Closed += (_, _) => { _loginWindow = null; };
        }

        desktop.MainWindow = _loginWindow;
        _mainWindow?.Hide();
        _loginWindow.Show();
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        if (_mainWindow == null)
        {
            // 通过 IoC 获取 MainWindow（构造中注入 MainWindowViewModel）
            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            _mainWindow = new MainWindow(mainVm);
            _mainWindow.Closed += (_, _) => { _mainWindow = null; };
        }

        desktop.MainWindow = _mainWindow;
        _loginWindow?.Hide();
        _mainWindow.Show();
    }
}
