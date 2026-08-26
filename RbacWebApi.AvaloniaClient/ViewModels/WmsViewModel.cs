using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.DTOs;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>WMS 模块主 VM：13 类实体统一在一个界面管理（左选择实体 + 右 DataGrid + 底部表单）</summary>
public partial class WmsViewModel : ViewModelBase
{
    private readonly IWmsService? _svc;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>左侧可选择的实体类型目录</summary>
    public ObservableCollection<WmsEntityType> EntityTypes { get; } = new(WmsEntityCatalog.All);

    /// <summary>当前列表数据（元素为对应 DTO 的 object）</summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <summary>编辑表单字段</summary>
    public ObservableCollection<WmsFieldVM> Fields { get; } = [];

    [ObservableProperty] private WmsEntityType? _selectedEntityType;
    [ObservableProperty] private object? _selectedItem;
    [ObservableProperty] private string _keyword = string.Empty;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private int _total;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editingTitle = string.Empty;

    /// <summary>当前实体 DTO 类型（供 View 重建 DataGrid 列）</summary>
    public Type? CurrentItemType { get; private set; }

    /// <summary>当前正在编辑的对象（新增/编辑共用）</summary>
    private object? _editingItem;

    public WmsViewModel()
    {
        DesignMode(() =>
        {
            var now = DateTimeOffset.Now;
            Items.Add(new WarehouseDto { Id = "01HW...", WarehouseCode = "WH01", WarehouseName = "中央仓", Status = 1, CreateTime = now });
            Items.Add(new WarehouseDto { Id = "01HX...", WarehouseCode = "WH02", WarehouseName = "华东仓", Status = 1, CreateTime = now.AddDays(-3) });
            Total = Items.Count;
            Status = $"共 {Total} 条（设计期示例）";
        });
    }

    public WmsViewModel(IWmsService svc) : this()
    {
        _svc = svc;
    }

    partial void OnSelectedEntityTypeChanged(WmsEntityType? value)
    {
        if (value == null) return;
        CurrentItemType = value.ItemType;
        OnPropertyChanged(nameof(CurrentItemType));
        Items.Clear();
        SelectedItem = null;
        IsEditing = false;
        if (_svc != null)
            _ = RefreshAsync();
    }

    // ============================================================
    //  查询
    // ============================================================

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_svc == null || SelectedEntityType == null) { Status = "服务未初始化"; return; }
        Status = "加载中...";
        var et = SelectedEntityType;
        var (ok, msg, items, total) = await _svc.GetListAsync(et.ItemType, et.Resource, new WmsQueryRequest
        {
            PageIndex = 1,
            PageSize = 100,
            Keyword = Keyword
        });
        Items.Clear();
        if (ok && items != null)
        {
            foreach (var it in items) Items.Add(it);
            Total = total;
            Status = $"共 {Total} 条记录";
        }
        else
        {
            Total = 0;
            Status = msg;
        }
    }

    // ============================================================
    //  新增 / 编辑：弹出底部表单
    // ============================================================

    [RelayCommand]
    private void AddNew()
    {
        if (SelectedEntityType == null) { Status = "请先选择实体类型"; return; }
        _editingItem = Activator.CreateInstance(SelectedEntityType.ItemType);
        EditingTitle = $"新增 {SelectedEntityType.Title}";
        BuildFields(_editingItem);
        IsEditing = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedItem == null || SelectedEntityType == null) { Status = "请先选择要编辑的行"; return; }
        // 克隆当前选中行，避免编辑中途影响列表显示
        _editingItem = Clone(SelectedItem);
        EditingTitle = $"编辑 {SelectedEntityType.Title}";
        BuildFields(_editingItem);
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        _editingItem = null;
        Fields.Clear();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_svc == null || SelectedEntityType == null || _editingItem == null) return;
        var et = SelectedEntityType;

        // 把表单字段写回对象
        WriteBackFields(_editingItem);

        var id = (string?)et.ItemType.GetProperty("Id")?.GetValue(_editingItem);

        if (string.IsNullOrEmpty(id))
        {
            var (ok, msg) = await _svc.CreateAsync(et.Resource, _editingItem);
            Status = ok ? "新增成功" : msg;
        }
        else
        {
            // 业务单据更新前先取回明细，避免清空已存在明细
            if (et.IsDocument)
            {
                var (dok, dmsg, ddata) = await _svc.GetWithDetailsAsync(et.ItemType, et.Resource, id);
                if (dok && ddata != null)
                {
                    var details = et.ItemType.GetProperty("Details")?.GetValue(ddata);
                    et.ItemType.GetProperty("Details")?.SetValue(_editingItem, details);
                }
            }
            var (ok, msg) = await _svc.UpdateAsync(et.Resource, id, _editingItem);
            Status = ok ? "更新成功" : msg;
        }

        IsEditing = false;
        _editingItem = null;
        Fields.Clear();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_svc == null || SelectedEntityType == null) { Status = "服务未初始化"; return; }
        if (SelectedItem == null) { Status = "请先选择要删除的行"; return; }
        var id = (string?)SelectedEntityType.ItemType.GetProperty("Id")?.GetValue(SelectedItem);
        if (string.IsNullOrEmpty(id)) { Status = "该行无 Id，无法删除"; return; }

        var (ok, msg) = await _svc.DeleteAsync(SelectedEntityType.Resource, id);
        Status = ok ? "删除成功" : msg;
        if (ok) { Items.Remove(SelectedItem); SelectedItem = null; Total = Items.Count; }
    }

    // ============================================================
    //  表单字段生成与回写（排除 Id/CreateTime/LastUpdateTime/Details）
    // ============================================================

    private void BuildFields(object target)
    {
        Fields.Clear();
        foreach (var p in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsSystemField(p.Name)) continue;
            var val = p.GetValue(target);
            Fields.Add(new WmsFieldVM
            {
                Label = p.Name,
                PropertyInfo = p,
                Text = val == null ? string.Empty : FormatValue(val)
            });
        }
    }

    private void WriteBackFields(object target)
    {
        foreach (var f in Fields)
        {
            if (f.PropertyInfo == null || !f.PropertyInfo.CanWrite) continue;
            var propType = f.PropertyInfo.PropertyType;
            var underlying = Nullable.GetUnderlyingType(propType) ?? propType;
            object? val = string.IsNullOrWhiteSpace(f.Text)
                ? (underlying.IsValueType && Nullable.GetUnderlyingType(propType) == null ? Activator.CreateInstance(underlying) : null)
                : Convert.ChangeType(f.Text, underlying);
            f.PropertyInfo.SetValue(target, val);
        }
    }

    private static bool IsSystemField(string name)
        => name is "Id" or "CreateTime" or "LastUpdateTime" or "Details";

    private static string FormatValue(object val) => val switch
    {
        bool b => b ? "1" : "0",
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm"),
        _ => val.ToString() ?? string.Empty
    };

    private static object Clone(object source)
    {
        var json = JsonSerializer.Serialize(source, source.GetType(), JsonOpt);
        return JsonSerializer.Deserialize(json, source.GetType(), JsonOpt)
               ?? throw new InvalidOperationException("克隆对象失败");
    }
}

/// <summary>动态表单字段：标签 + 文本（双向绑定到 TextBox）</summary>
public partial class WmsFieldVM : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public PropertyInfo? PropertyInfo { get; set; }
    [ObservableProperty] private string _text = string.Empty;
}
