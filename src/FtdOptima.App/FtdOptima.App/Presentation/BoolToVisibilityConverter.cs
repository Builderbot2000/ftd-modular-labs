using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FtdOptima.App.Presentation;

/// <summary>Maps a bool to <see cref="Visibility"/> (true =&gt; Visible). WinUI has no built-in.</summary>
public partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}
