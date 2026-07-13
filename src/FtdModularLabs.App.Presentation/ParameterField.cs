using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtdModularLabs.Core;

namespace FtdModularLabs.App.Presentation;

/// <summary>One entry in a <see cref="ParameterKind.LayerStack"/> field (e.g. an armor layer).</summary>
public sealed class LayerStackEntry
{
    public LayerStackEntry(string name, ParameterField owner)
    {
        Name = name;
        Owner = owner;
    }

    public string Name { get; }

    /// <summary>The field that owns this entry — lets item templates bind its reorder/remove commands.</summary>
    public ParameterField Owner { get; }
}

/// <summary>
/// An editable, bindable wrapper around one <see cref="ParameterDescriptor"/>. The generic form
/// binds each control's value here; <see cref="ModuleRunnerViewModel"/> reads <see cref="EffectiveValue"/>
/// back out when computing. Exposed properties cover every renderer path so a single
/// <c>DataTemplateSelector</c> keyed on <see cref="Kind"/> can bind against one field type.
/// </summary>
public partial class ParameterField : ObservableObject
{
    public ParameterField(ParameterDescriptor descriptor)
    {
        Descriptor = descriptor;
        value = descriptor.Default;
        if (Kind == ParameterKind.Enum || Kind == ParameterKind.LayerStack)
        {
            selectedOptionToAdd = descriptor.Options is { Count: > 0 } ? descriptor.Options[0] : null;
        }
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

    // ---- LayerStack (armor builder) support --------------------------------------------------

    /// <summary>The ordered stack of chosen options (front → back), for a LayerStack field.</summary>
    public ObservableCollection<LayerStackEntry> Stack { get; } = new();

    /// <summary>The option currently selected in the "add" picker.</summary>
    [ObservableProperty]
    private string? selectedOptionToAdd;

    /// <summary>
    /// The value handed to the module: the ordered option names for a LayerStack, otherwise
    /// the scalar <see cref="Value"/>.
    /// </summary>
    public object? EffectiveValue =>
        Kind == ParameterKind.LayerStack ? Stack.Select(e => e.Name).ToList() : Value;

    [RelayCommand]
    private void AddLayer()
    {
        var toAdd = SelectedOptionToAdd ?? (Options.Count > 0 ? Options[0] : null);
        if (toAdd is not null)
        {
            Stack.Add(new LayerStackEntry(toAdd, this));
        }
    }

    [RelayCommand]
    private void RemoveLayer(LayerStackEntry? entry)
    {
        if (entry is not null)
        {
            Stack.Remove(entry);
        }
    }

    [RelayCommand]
    private void MoveLayerUp(LayerStackEntry? entry)
    {
        if (entry is null)
        {
            return;
        }
        int i = Stack.IndexOf(entry);
        if (i > 0)
        {
            Stack.Move(i, i - 1);
        }
    }

    [RelayCommand]
    private void MoveLayerDown(LayerStackEntry? entry)
    {
        if (entry is null)
        {
            return;
        }
        int i = Stack.IndexOf(entry);
        if (i >= 0 && i < Stack.Count - 1)
        {
            Stack.Move(i, i + 1);
        }
    }
}
