using FtdOptima.Core;

namespace FtdOptima.App.Presentation;

/// <summary>
/// An editable, bindable wrapper around one <see cref="ParameterDescriptor"/>. The generic form
/// binds each control's value here; <see cref="ModuleRunnerViewModel"/> reads <see cref="Value"/>
/// back out when computing. Exposed properties cover every renderer path so a single
/// <c>DataTemplateSelector</c> keyed on <see cref="Kind"/> can bind against one field type.
/// </summary>
public partial class ParameterField : ObservableObject
{
    public ParameterField(ParameterDescriptor descriptor)
    {
        Descriptor = descriptor;
        value = descriptor.Default;
    }

    public ParameterDescriptor Descriptor { get; }

    public string Key => Descriptor.Key;
    public string Label => Descriptor.Label;
    public ParameterKind Kind => Descriptor.Kind;
    public string? Unit => Descriptor.Unit;
    public string? Help => Descriptor.Help;

    /// <summary>Label with unit appended, e.g. "Muzzle velocity (m/s)".</summary>
    public string DisplayLabel => string.IsNullOrEmpty(Unit) ? Label : $"{Label} ({Unit})";

    public IReadOnlyList<string> Options => Descriptor.Options ?? Array.Empty<string>();

    public double? Min => Descriptor.Min;
    public double? Max => Descriptor.Max;

    [ObservableProperty]
    private object? value;
}
