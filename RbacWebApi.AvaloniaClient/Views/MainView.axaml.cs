using Avalonia.Controls;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public MainView(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
