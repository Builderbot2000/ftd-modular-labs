namespace FtdModularLabs.Core;

/// <summary>
/// A simple columnar result table. Flexible enough for the future APS per-loader-size table.
/// </summary>
public sealed class ResultTable
{
    public ResultTable(
        string title,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<object>> rows)
    {
        Title = title;
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public string Title { get; }
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<IReadOnlyList<object>> Rows { get; }
}
