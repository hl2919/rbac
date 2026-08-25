using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.AvaloniaClient.Views;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService? _auth;

    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "123456";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _isLoading;

    [ObservableProperty] private string _status = string.Empty;

    /// <summary>登录成功后通知外层切换页面</summary>
    public event Action? LoginSuccess;

    /// <summary>设计期无参构造：仅填充示例数据，不访问真实服务</summary>
    public LoginViewModel()
    {
        DesignMode(() =>
        {
            Username = "admin";
            Password = "******";
            Status = "在此输入用户名和密码";
        });
    }

    /// <summary>纯注入构造（运行时由 IoC 调用）</summary>
    public LoginViewModel(IAuthService auth) : this()
    {
        _auth = auth;
    }

    private bool CanExecuteWhenNotLoading => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanExecuteWhenNotLoading))]
    private async Task LoginAsync()
    {
        if (_auth == null) { Status = "服务未初始化"; return; }
        IsLoading = true;
        Status = "登录中...";
        var (ok, msg) = await _auth.LoginAsync(Username, Password);
        IsLoading = false;
        Status = ok ? "登录成功" : msg;
        if (ok) LoginSuccess?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteWhenNotLoading))]
    private async Task RegisterAsync()
    {
        if (_auth == null) { Status = "服务未初始化"; return; }
        IsLoading = true;
        Status = "注册中...";
        var (ok, msg) = await _auth.RegisterAsync(new RegisterRequest
        {
            Username = Username,
            Password = Password
        });
        IsLoading = false;
        Status = ok ? "注册成功，请点击登录" : msg;
    }
}
