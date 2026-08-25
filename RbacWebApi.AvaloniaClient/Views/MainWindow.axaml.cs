using Avalonia.Controls;
using Avalonia.Interactivity;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class MainWindow : Window
{
    /// <summary>设计器回退</summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>IoC 构造：注入 MainWindowViewModel 并设置自身 DataContext</summary>
    public MainWindow(MainWindowViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // 刷新所有模块数据
            await vm.MainVm.RefreshAllCommand.ExecuteAsync(null);
        }
    }
}
