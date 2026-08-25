using Avalonia.Controls;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
    }

    public UserManagementView(UserManagementViewModel vm) : this()
    {
        DataContext = vm;
    }
}
