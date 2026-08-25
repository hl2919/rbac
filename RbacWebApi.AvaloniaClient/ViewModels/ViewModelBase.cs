using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>基类：提供设计期辅助与统一初始化入口</summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>当前是否运行在设计器中（ViewModel 无参构造里用）</summary>
    protected static bool IsDesignMode => Avalonia.Controls.Design.IsDesignMode;

    /// <summary>
    /// 设计期数据填充：仅当 IsDesignMode == true 时执行指定 action。
    /// 规范：所有 ViewModel 无参构造中，"示例数据注入"必须包在此方法内。
    /// </summary>
    protected static void DesignMode(Action action)
    {
        if (IsDesignMode) action?.Invoke();
    }
}
