using System.Collections.ObjectModel;
using FtdOptima.App.Presentation;
using FtdOptima.Core;
using FtdOptima.Domain.Catalog;
using FtdOptima.Domain.Model;
using FtdOptima.Domain.Serialization;

namespace FtdOptima.App.Presentation;

/// <summary>
/// Edits one <see cref="DesignModule"/>: builds the generic parameter form from its subsystem
/// type's calculator schema, seeds it from the module's saved values, runs the calculator when one
/// exists (else shows a "coming soon" notice), and commits edited values back into the module.
/// </summary>
public partial class ModuleEditorViewModel : ObservableObject
{
    private readonly DesignModule _module;
    private readonly ICalculationModule? _calculator;
    private readonly ModuleSchema? _schema;

    public ModuleEditorViewModel(DesignModule module, ISubsystemTypeRegistry registry)
    {
        _module = module;
        var type = registry.FindType(module.SubsystemTypeId);
        _calculator = registry.GetCalculator(module.SubsystemTypeId);
        _schema = _calculator?.InputSchema;

        TypeName = type?.Name ?? module.SubsystemTypeId;
        Category = type?.Category.ToString() ?? "";
        Description = _calculator?.Description ?? type?.Description ?? "";

        Fields = new ObservableCollection<ParameterField>();
        if (_schema is not null)
        {
            var saved = ParameterValueSnapshot.Restore(_module.Values, _schema);
            foreach (var p in _schema.Parameters)
                Fields.Add(BuildField(p, saved));
        }

        ComputeCommand = new AsyncRelayCommand(ComputeAsync, () => HasCalculator);
    }

    public string TypeName { get; }
    public string Category { get; }
    public string Description { get; }

    /// <summary>True when this module's subsystem type has a calculator wired.</summary>
    public bool HasCalculator => _calculator is not null;

    /// <summary>True when there is no calculator yet — the UI shows the "coming soon" panel.</summary>
    public bool ComingSoon => _calculator is null;

    public string ModuleName => _module.Name;
    public string SubsystemTypeId => _module.SubsystemTypeId;

    public ObservableCollection<ParameterField> Fields { get; }

    public ICommand ComputeCommand { get; }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasResult;

    [ObservableProperty]
    private string? notes;

    public ObservableCollection<SummaryCard> Summary { get; } = new();
    public ObservableCollection<ResultTableView> Tables { get; } = new();

    /// <summary>Writes the current field values back into the module's persisted value snapshot.</summary>
    public void CommitToModule()
    {
        if (_schema is null)
            return;
        var values = new ParameterValues();
        foreach (var field in Fields)
            values.Set(field.Key, field.EffectiveValue);

        var captured = ParameterValueSnapshot.Capture(values, _schema);
        _module.Values.Clear();
        foreach (var (key, value) in captured)
            _module.Values[key] = value;
    }

    private static ParameterField BuildField(ParameterDescriptor descriptor, ParameterValues saved)
    {
        var field = new ParameterField(descriptor);
        if (!saved.Contains(descriptor.Key))
            return field;

        if (descriptor.Kind == ParameterKind.LayerStack)
        {
            foreach (var name in saved.GetStringList(descriptor.Key))
                field.Stack.Add(new LayerStackEntry(name, field));
        }
        else
        {
            field.Value = descriptor.Kind switch
            {
                ParameterKind.Number or ParameterKind.Integer => saved.GetDouble(descriptor.Key),
                ParameterKind.Boolean => saved.GetBool(descriptor.Key),
                _ => saved.GetString(descriptor.Key),
            };
        }
        return field;
    }

    private async Task ComputeAsync()
    {
        if (IsBusy || _calculator is null || _schema is null)
            return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var values = new ParameterValues();
            foreach (var field in Fields)
                values.Set(field.Key, field.EffectiveValue);

            var errors = values.Validate(_schema);
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                HasResult = false;
                return;
            }

            var result = await _calculator.ComputeAsync(values, CancellationToken.None);

            Summary.Clear();
            foreach (var kvp in result.Summary)
                Summary.Add(new SummaryCard(
                    kvp.Key,
                    Convert.ToString(kvp.Value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty));

            Tables.Clear();
            foreach (var table in result.Tables)
                Tables.Add(new ResultTableView(table));

            Notes = result.Notes;
            HasResult = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasResult = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
