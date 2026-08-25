using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace RbacWebApi.AvaloniaClient;

class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        // 在 Avalonia 启动前完成 IoC 容器构建
        var sp = new ServiceCollection()
            .ConfigureServices()
            .BuildServiceProvider();
        Services = sp;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
