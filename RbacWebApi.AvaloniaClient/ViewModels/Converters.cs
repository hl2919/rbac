using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace RbacWebApi.AvaloniaClient.ViewModels;

/// <summary>布尔到文本：true → 文件夹, false → 文件</summary>
public class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "📁" : "📄";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
