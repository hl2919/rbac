using Avalonia.Controls;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class LoginView : UserControl
{
    /// <summary>设计器 / XAML 回退构造（无 DataContext，依赖外部设置）</summary>
    public LoginView()
    {
        InitializeComponent();
    }

    /// <summary>IoC 构造：注入 ViewModel 并设置为自身 DataContext</summary>
    public LoginView(LoginViewModel vm) : this()
    {
        DataContext = vm;
    }
}
