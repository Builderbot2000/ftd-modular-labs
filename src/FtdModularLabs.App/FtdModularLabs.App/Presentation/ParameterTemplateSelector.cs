using FtdModularLabs.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FtdModularLabs.App.Presentation;

/// <summary>
/// Picks an editor template for a <see cref="ParameterField"/> based on its
/// <see cref="ParameterField.Kind"/>. Templates are supplied from XAML so the shell renders a
/// form for any module without knowing the module.
/// </summary>
public partial class ParameterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NumberTemplate { get; set; }
    public DataTemplate? IntegerTemplate { get; set; }
    public DataTemplate? EnumTemplate { get; set; }
    public DataTemplate? BooleanTemplate { get; set; }
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? LayerStackTemplate { get; set; }
    public DataTemplate? ModuleReferenceTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) => Select(item);

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) => Select(item);

    private DataTemplate? Select(object item) => item is ParameterField field
        ? field.Kind switch
        {
            ParameterKind.Number => NumberTemplate,
            ParameterKind.Integer => IntegerTemplate ?? NumberTemplate,
            ParameterKind.Enum => EnumTemplate,
            ParameterKind.Boolean => BooleanTemplate,
            ParameterKind.Text => TextTemplate,
            ParameterKind.LayerStack => LayerStackTemplate ?? TextTemplate,
            ParameterKind.ModuleReference => ModuleReferenceTemplate ?? EnumTemplate ?? TextTemplate,
            _ => TextTemplate,
        }
        : null;
}
