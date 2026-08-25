using Avalonia.Controls;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class RoleManagementView : UserControl
{
    public RoleManagementView()
    {
        InitializeComponent();
    }

    public RoleManagementView(RoleManagementViewModel vm) : this()
    {
        DataContext = vm;
    }
}
