using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;
using RbacWebApi.AvaloniaClient.ViewModels;

namespace RbacWebApi.AvaloniaClient.Views;

public partial class WmsView : UserControl
{
    private INotifyPropertyChanged? _currentVm;

    public WmsView()
    {
        InitializeComponent();
        Loaded += (_, _) => RebuildColumns();
    }

    public WmsView(WmsViewModel vm) : this()
    {
        DataContext = vm;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_currentVm != null)
            _currentVm.PropertyChanged -= Vm_PropertyChanged;

        _currentVm = DataContext as INotifyPropertyChanged;
        if (_currentVm != null)
            _currentVm.PropertyChanged += Vm_PropertyChanged;

        RebuildColumns();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WmsViewModel.CurrentItemType))
            RebuildColumns();
    }

    /// <summary>按当前实体 DTO 类型重建 DataGrid 列（排除 Details；系统字段只读）</summary>
    private void RebuildColumns()
    {
        var grid = this.FindControl<DataGrid>("WmsGrid");
        if (grid == null) return;
        grid.Columns.Clear();

        if (DataContext is not WmsViewModel vm || vm.CurrentItemType == null) return;

        foreach (var p in vm.CurrentItemType.GetProperties())
        {
            if (p.Name is "Details") continue;
            var col = new DataGridTextColumn
            {
                Header = p.Name,
                Binding = new Binding(p.Name),
                IsReadOnly = p.Name is "Id" or "CreateTime" or "LastUpdateTime"
            };
            if (p.Name == "Id")
                col.Width = new DataGridLength(220);
            grid.Columns.Add(col);
        }
    }
}
