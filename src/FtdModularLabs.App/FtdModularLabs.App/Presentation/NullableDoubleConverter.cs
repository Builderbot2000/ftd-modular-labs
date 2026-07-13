using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace FtdModularLabs.App.Presentation;

/// <summary>
/// Formats a <see cref="double"/>? for display. <c>null</c> renders as an em-dash so a stat that
/// no module contributes to reads as "unknown" rather than a misleading zero. The
/// <see cref="ConverterParameter"/> is a standard numeric format string (e.g. "N0", "N2");
/// omitted → "N1".
/// </summary>
public partial class NullableDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not double d)
            return "—";
        var format = parameter as string;
        if (string.IsNullOrEmpty(format))
            format = "N1";
        return d.ToString(format, CultureInfo.CurrentCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
