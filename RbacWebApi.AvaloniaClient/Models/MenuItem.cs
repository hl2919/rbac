using System.Collections.ObjectModel;

namespace RbacWebApi.AvaloniaClient.Models;

/// <summary>菜单项模型：支持层级嵌套，方便后续扩展</summary>
public class MenuItem
{
    /// <summary>菜单显示标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>图标字符（emoji 或字体图标）</summary>
    public string? Icon { get; set; }

    /// <summary>唯一标识，用于切换 View</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>子菜单（为空表示叶子节点）</summary>
    public ObservableCollection<MenuItem> Children { get; set; } = [];

    /// <summary>是否为叶子节点（无子菜单）</summary>
    public bool IsLeaf => Children.Count == 0;
}
