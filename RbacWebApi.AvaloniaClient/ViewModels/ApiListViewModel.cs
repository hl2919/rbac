using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RbacWebApi.AvaloniaClient.Models;
using RbacWebApi.AvaloniaClient.Services;
using RbacWebApi.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>API 列表</summary>
public partial class ApiListViewModel : ViewModelBase
{
    private readonly IApiResourceService? _svc;
    public ObservableCollection<SysApiDto> Items { get; } = [];

    [ObservableProperty] private string _status = string.Empty;

    /// <summary>设计期无参构造：填充示例 API 资源数据</summary>
    public ApiListViewModel()
    {
        DesignMode(() =>
        {
            var now = DateTimeOffset.Now;
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M10", ApiUrl = "/api/auth/login", RequestMethod = "POST", ApiName = "用户登录", NeedAuth = false, CreateTime = now.AddDays(-30) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M11", ApiUrl = "/api/auth/register", RequestMethod = "POST", ApiName = "用户注册", NeedAuth = false, CreateTime = now.AddDays(-30) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M12", ApiUrl = "/api/auth/me", RequestMethod = "GET", ApiName = "获取当前用户", NeedAuth = true, CreateTime = now.AddDays(-28) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M13", ApiUrl = "/api/users", RequestMethod = "GET", ApiName = "用户列表", NeedAuth = true, CreateTime = now.AddDays(-25) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M14", ApiUrl = "/api/users/{id}", RequestMethod = "PUT", ApiName = "更新用户", NeedAuth = true, CreateTime = now.AddDays(-25) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M15", ApiUrl = "/api/roles", RequestMethod = "GET", ApiName = "角色列表", NeedAuth = true, CreateTime = now.AddDays(-20) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M16", ApiUrl = "/api/roles/{id}/permissions", RequestMethod = "GET", ApiName = "角色权限明细", NeedAuth = true, CreateTime = now.AddDays(-18) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M17", ApiUrl = "/api/apis", RequestMethod = "GET", ApiName = "API 资源列表", NeedAuth = true, CreateTime = now.AddDays(-15) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M18", ApiUrl = "/api/cloud/activate", RequestMethod = "POST", ApiName = "开通云盘", NeedAuth = true, CreateTime = now.AddDays(-10) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M19", ApiUrl = "/api/cloud/files", RequestMethod = "GET", ApiName = "云盘文件列表", NeedAuth = true, CreateTime = now.AddDays(-8) });
            Items.Add(new SysApiDto { Id = "01AN4Z07BY79KA1307SR9X4M1A", ApiUrl = "/api/cloud/upload/init", RequestMethod = "POST", ApiName = "初始化上传", NeedAuth = true, CreateTime = now.AddDays(-5) });
            Status = $"共 {Items.Count} 个接口（设计期示例）";
        });
    }

    /// <summary>IoC 注入构造</summary>
    public ApiListViewModel(IApiResourceService svc) : this()
    {
        _svc = svc;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_svc == null) { Status = "服务未初始化"; return; }
        Status = "加载中...";
        var (ok, msg, data) = await _svc.GetListAsync(new PageKeyRequest { PageIndex = 1, PageSize = 200 });
        Items.Clear();
        if (ok && data != null)
        {
            foreach (var a in data.Items) Items.Add(a);
            Status = $"共 {data.Total} 个接口";
        }
        else Status = msg;
    }
}
