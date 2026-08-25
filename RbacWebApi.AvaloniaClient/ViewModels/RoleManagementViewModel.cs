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

/// <summary>角色管理</summary>
public partial class RoleManagementViewModel : ViewModelBase
{
    private readonly IRoleService? _svc;
    public ObservableCollection<SysRoleDto> Items { get; } = [];

    [ObservableProperty] private string _status = string.Empty;

    /// <summary>设计期无参构造：填充示例角色数据</summary>
    public RoleManagementViewModel()
    {
        DesignMode(() =>
        {
            var now = DateTimeOffset.Now;
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVA", RoleName = "超级管理员", RoleCode = "super_admin", Description = "拥有系统所有权限", Status = 1, CreateTime = now.AddDays(-90) });
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVB", RoleName = "管理员", RoleCode = "admin", Description = "系统配置、用户管理权限", Status = 1, CreateTime = now.AddDays(-60) });
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVC", RoleName = "运营", RoleCode = "operator", Description = "内容运营、数据查看权限", Status = 1, CreateTime = now.AddDays(-30) });
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVD", RoleName = "编辑", RoleCode = "editor", Description = "内容编辑权限", Status = 1, CreateTime = now.AddDays(-20) });
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVE", RoleName = "普通用户", RoleCode = "user", Description = "基础功能访问", Status = 1, CreateTime = now.AddDays(-10) });
            Items.Add(new SysRoleDto { Id = "01AN4Z07BY79KA1307SR9X4MVF", RoleName = "访客", RoleCode = "guest", Description = "只读权限（已禁用）", Status = 0, CreateTime = now.AddDays(-5) });
            Status = $"共 {Items.Count} 个角色（设计期示例）";
        });
    }

    /// <summary>IoC 注入构造</summary>
    public RoleManagementViewModel(IRoleService svc) : this()
    {
        _svc = svc;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_svc == null) { Status = "服务未初始化"; return; }
        Status = "加载中...";
        var (ok, msg, data) = await _svc.GetListAsync(new PageKeyRequest { PageIndex = 1, PageSize = 100 });
        Items.Clear();
        if (ok && data != null)
        {
            foreach (var r in data.Items) Items.Add(r);
            Status = $"共 {data.Total} 个角色";
        }
        else Status = msg;
    }
}
