using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.AvaloniaClient.Views;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>主窗口 VM：用户信息 + 退出登录</summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService? _auth;

    public MainViewModel MainVm { get; }

    public string WindowTitle => "RBAC + 云盘 管理客户端";

    public string CurrentUserInfo => _auth?.Current != null
        ? $"{_auth.Current.Nickname ?? _auth.Current.Username} ({string.Join(",", _auth.Current.Roles)})"
        : "管理员 (Admin)";

    /// <summary>设计期无参构造：使用 MainViewModel 示例数据，CurrentUserInfo 返回占位</summary>
    public MainWindowViewModel()
    {
        MainVm = new MainViewModel();
        DesignMode(() =>
        {
            // 设计期无需订阅事件
        });
    }

    /// <summary>IoC 注入构造</summary>
    public MainWindowViewModel(IAuthService auth, MainViewModel mainVm) : this()
    {
        _auth = auth;
        MainVm = mainVm;
        _auth.StateChanged += () => OnPropertyChanged(nameof(CurrentUserInfo));
    }

    /// <summary>退出登录：由 App 层监听并切换回 LoginWindow</summary>
    [RelayCommand]
    private void Logout()
    {
        _auth?.Logout();
    }
}

/// <summary>主内容 VM：左侧菜单 + 右侧内容区切换</summary>
public partial class MainViewModel : ViewModelBase
{
    private UserManagementView? _userView;
    private RoleManagementView? _roleView;
    private ApiListView? _apiView;
    private CloudView? _cloudView;
    private UserManagementViewModel? _userVm;
    private RoleManagementViewModel? _roleVm;
    private ApiListViewModel? _apiVm;
    private CloudViewModel? _cloudVm;

    /// <summary>菜单项集合（动态加载，方便后续扩展）</summary>
    public ObservableCollection<MenuItem> MenuItems { get; } = [];

    /// <summary>当前选中的菜单项</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectMenuItemCommand))]
    private MenuItem? _selectedMenuItem;

    /// <summary>当前显示的 View</summary>
    [ObservableProperty] private object? _currentView;

    /// <summary>设计期无参构造：填充菜单 + 示例数据，右栏默认显示用户管理示例 View</summary>
    public MainViewModel()
    {
        LoadMenuItems();

        DesignMode(() =>
        {
            // 设计期：用无参构造的子 View（自带示例数据）
            var uv = new UserManagementView();
            var rv = new RoleManagementView();
            var av = new ApiListView();
            var cv = new CloudView();
            var uvm = new UserManagementViewModel();
            var rvm = new RoleManagementViewModel();
            var avm = new ApiListViewModel();
            var cvm = new CloudViewModel();
            uv.DataContext = uvm;
            rv.DataContext = rvm;
            av.DataContext = avm;
            cv.DataContext = cvm;
            _userView = uv;
            _roleView = rv;
            _apiView = av;
            _cloudView = cv;
            _userVm = uvm;
            _roleVm = rvm;
            _apiVm = avm;
            _cloudVm = cvm;

            if (MenuItems.Count > 0)
                SelectMenuItem(MenuItems[0]);
        });
    }

    /// <summary>IoC 注入构造：运行期由容器提供 View 和 VM 实例</summary>
    public MainViewModel(
        UserManagementView userView, RoleManagementView roleView,
        ApiListView apiView, CloudView cloudView,
        UserManagementViewModel userVm, RoleManagementViewModel roleVm,
        ApiListViewModel apiVm, CloudViewModel cloudVm) : this()
    {
        _userView = userView;
        _roleView = roleView;
        _apiView = apiView;
        _cloudView = cloudView;
        _userVm = userVm;
        _roleVm = roleVm;
        _apiVm = apiVm;
        _cloudVm = cloudVm;

        // 运行期再设置一次：避免设计期 View 实例被覆盖时需要重新绑定
        if (MenuItems.Count > 0 && SelectedMenuItem == null)
            SelectMenuItem(MenuItems[0]);
    }

    /// <summary>加载菜单数据：后续新增菜单只需在此处添加即可</summary>
    private void LoadMenuItems()
    {
        MenuItems.Add(new MenuItem { Title = "用户管理", Icon = "👥", Key = "users" });
        MenuItems.Add(new MenuItem { Title = "角色管理", Icon = "🔑", Key = "roles" });
        MenuItems.Add(new MenuItem { Title = "API 资源", Icon = "🔌", Key = "apis" });
        MenuItems.Add(new MenuItem { Title = "我的云盘", Icon = "☁️", Key = "cloud" });
    }

    /// <summary>根据菜单 Key 切换右侧内容 View</summary>
    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        if (value == null) return;
        SelectMenuItem(value);
    }

    [RelayCommand]
    private void SelectMenuItem(MenuItem item)
    {
        CurrentView = item.Key switch
        {
            "users"  => _userView,
            "roles"  => _roleView,
            "apis"   => _apiView,
            "cloud"  => _cloudView,
            _        => null
        };
    }

    /// <summary>刷新所有模块数据</summary>
    [RelayCommand]
    public async Task RefreshAllAsync()
    {
        if (_userVm != null)  await _userVm.RefreshAsync();
        if (_roleVm != null)  await _roleVm.RefreshAsync();
        if (_apiVm != null)   await _apiVm.RefreshAsync();
        if (_cloudVm != null) await _cloudVm.RefreshStatusAsync();
    }
}
