using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FtdOptima.App.Presentation;

/// <summary>
/// Maps a bool to <see cref="Visibility"/> (true =&gt; Visible). WinUI has no built-in. Pass
/// ConverterParameter="invert" to flip the mapping (true =&gt; Collapsed).
/// </summary>
public partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is true;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
            flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}
