using Microsoft.Extensions.DependencyInjection;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.AvaloniaClient.ViewModels;
using RbacWebApi.AvaloniaClient.Views;

namespace RbacWebApi.AvaloniaClient;

/// <summary>IoC 容器注册：Services、ViewModels、Views</summary>
public static class ServiceConfigurator
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // ===== 基础服务（Singleton，整个生命周期共享） =====
        services.AddSingleton<ApiSettings>();
        services.AddSingleton<ApiClient>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IRoleService, RoleService>();
        services.AddSingleton<IApiResourceService, ApiResourceService>();
        services.AddSingleton<ICloudDiskService, CloudDiskService>();
        services.AddSingleton<IFileApiService, FileApiService>();

        // ===== ViewModels（Singleton：保留运行期状态） =====
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<UserManagementViewModel>();
        services.AddSingleton<RoleManagementViewModel>();
        services.AddSingleton<ApiListViewModel>();
        services.AddSingleton<CloudViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // ===== Views（Transient：每次获取创建新实例；构造中注入对应 VM 并设置 DataContext） =====
        services.AddTransient<UserManagementView>();
        services.AddTransient<RoleManagementView>();
        services.AddTransient<ApiListView>();
        services.AddTransient<CloudView>();
        services.AddTransient<MainView>();

        return services;
    }
}
