using FtdOptima.Core;

namespace FtdOptima.App.Presentation;

/// <summary>A scalar KPI rendered as a card in the results view.</summary>
public sealed record SummaryCard(string Label, string Value);

/// <summary>A <see cref="ResultTable"/> flattened to strings for the generic results view.</summary>
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
