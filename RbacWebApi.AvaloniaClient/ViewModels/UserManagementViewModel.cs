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

/// <summary>用户管理</summary>
public partial class UserManagementViewModel : ViewModelBase
{
    private readonly IUserService? _svc;

    public ObservableCollection<SysUserDto> Items { get; } = [];

    [ObservableProperty] private string _keyword = string.Empty;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _total;
    [ObservableProperty] private string _status = string.Empty;

    /// <summary>设计期无参构造：填充示例用户数据供预览</summary>
    public UserManagementViewModel()
    {
        DesignMode(() =>
        {
            var now = DateTimeOffset.Now;
            Items.Add(new SysUserDto { Id = "01AN4Z07BY79KA1307SR9X4MV3", Username = "admin", Nickname = "超级管理员", Email = "admin@demo.com", Phone = "13800138000", Status = 1, CreateTime = now.AddDays(-30) });
            Items.Add(new SysUserDto { Id = "01AN4Z07BY79KA1307SR9X4MV4", Username = "manager", Nickname = "运营主管", Email = "manager@demo.com", Phone = "13800138001", Status = 1, CreateTime = now.AddDays(-12) });
            Items.Add(new SysUserDto { Id = "01AN4Z07BY79KA1307SR9X4MV5", Username = "editor", Nickname = "编辑", Email = "editor@demo.com", Phone = "13800138002", Status = 1, CreateTime = now.AddDays(-5) });
            Items.Add(new SysUserDto { Id = "01AN4Z07BY79KA1307SR9X4MV6", Username = "guest", Nickname = "访客用户", Email = "guest@demo.com", Phone = "13800138003", Status = 0, CreateTime = now.AddDays(-2) });
            Items.Add(new SysUserDto { Id = "01AN4Z07BY79KA1307SR9X4MV7", Username = "tester", Nickname = "测试账号", Email = "tester@demo.com", Phone = "13800138004", Status = 1, CreateTime = now.AddHours(-8) });
            Total = 128;
            Status = $"共 {Total} 条记录（设计期示例）";
        });
    }

    /// <summary>IoC 注入构造</summary>
    public UserManagementViewModel(IUserService svc) : this()
    {
        _svc = svc;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_svc == null) { Status = "服务未初始化"; return; }
        Status = "加载中...";
        var (ok, msg, data) = await _svc.GetListAsync(new PageKeyRequest
        {
            Keyword = Keyword,
            PageIndex = PageIndex,
            PageSize = PageSize
        });
        Items.Clear();
        if (ok && data != null)
        {
            Total = data.Total;
            foreach (var u in data.Items) Items.Add(u);
            Status = $"共 {Total} 条记录";
        }
        else
        {
            Total = 0;
            Status = msg;
        }
    }

    [RelayCommand]
    private async Task PreviousAsync()
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (PageIndex * PageSize < Total)
        {
            PageIndex++;
            await RefreshAsync();
        }
    }
}
