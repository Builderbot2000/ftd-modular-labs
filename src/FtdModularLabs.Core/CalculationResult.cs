namespace FtdModularLabs.Core;

/// <summary>
/// The output of a module computation: scalar KPIs, zero or more tables, and optional notes.
/// </summary>
public sealed class CalculationResult
{
    public CalculationResult(
        IReadOnlyDictionary<string, object> summary,
        IReadOnlyList<ResultTable>? tables = null,
        string? notes = null)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Tables = tables ?? Array.Empty<ResultTable>();
        Notes = notes;
    }

    /// <summary>Scalar key/value KPIs, rendered as cards by the shell.</summary>
    public IReadOnlyDictionary<string, object> Summary { get; }

    public IReadOnlyList<ResultTable> Tables { get; }

    public string? Notes { get; }
}
