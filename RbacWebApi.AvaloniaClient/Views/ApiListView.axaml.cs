using Avalonia.Controls;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class ApiListView : UserControl
{
    public ApiListView()
    {
        InitializeComponent();
    }

    public ApiListView(ApiListViewModel vm) : this()
    {
        DataContext = vm;
    }
}
