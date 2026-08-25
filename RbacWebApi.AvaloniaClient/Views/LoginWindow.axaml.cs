using Avalonia.Controls;
using Avalonia.Interactivity;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    public LoginWindow(LoginViewModel vm) : this()
    {
        DataContext = vm;
    }
}
