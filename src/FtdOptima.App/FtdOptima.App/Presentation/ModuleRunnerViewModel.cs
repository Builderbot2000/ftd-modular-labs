using System.Collections.ObjectModel;
using FtdOptima.Core;

namespace FtdOptima.App.Presentation;

/// <summary>A scalar KPI rendered as a card.</summary>
public sealed record SummaryCard(string Label, string Value);

/// <summary>A table flattened to strings for display in the generic results view.</summary>
public sealed class ResultTableView
{
    public ResultTableView(ResultTable table)
    {
        Title = table.Title;
        Columns = table.Columns.ToList();
        Rows = table.Rows
            .Select(r => (IReadOnlyList<string>)r
                .Select(c => Convert.ToString(c, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)
                .ToList())
            .ToList();
    }

    public string Title { get; }
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}

/// <summary>
/// Drives one module: builds an editable form from its <see cref="ICalculationModule.InputSchema"/>,
/// runs <see cref="ICalculationModule.ComputeAsync"/>, and exposes the results for the shell to render.
/// </summary>
public partial class ModuleRunnerViewModel : ObservableObject
{
    private readonly ICalculationModule _module;

    public ModuleRunnerViewModel(ICalculationModule module)
    {
        _module = module;
        Fields = new ObservableCollection<ParameterField>(
            module.InputSchema.Parameters.Select(p => new ParameterField(p)));
        ComputeCommand = new AsyncRelayCommand(ComputeAsync);
    }

    public string Name => _module.Name;
    public string Description => _module.Description;

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

    private async Task ComputeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var values = new ParameterValues();
            foreach (var field in Fields)
                values.Set(field.Key, field.Value);

            var errors = values.Validate(_module.InputSchema);
            if (errors.Count > 0)
            {
                ErrorMessage = string.Join("\n", errors);
                HasResult = false;
                return;
            }

            var result = await _module.ComputeAsync(values, CancellationToken.None);

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
