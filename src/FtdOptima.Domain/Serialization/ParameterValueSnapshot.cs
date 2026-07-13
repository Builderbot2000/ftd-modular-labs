using System.Globalization;
using System.Text.Json;
using FtdOptima.Core;

namespace FtdOptima.Domain.Serialization;

/// <summary>
/// Bridges a module's live <see cref="ParameterValues"/> and its persisted, JSON-friendly value
/// snapshot. Normalization is <em>schema-driven</em>: each value is coerced to the CLR type its
/// <see cref="ParameterKind"/> dictates, so the round-trip survives both boxed primitives (from UI
/// fields) and <see cref="JsonElement"/>s (from deserialized JSON), which the raw
/// <see cref="ParameterValues"/> getters do not accept.
/// </summary>
public static class ParameterValueSnapshot
{
    /// <summary>
    /// Captures the values relevant to <paramref name="schema"/> as normalized primitives
    /// (double / int / bool / string, or <see cref="List{String}"/> for a LayerStack). Values not in
    /// the schema, or absent, are dropped — defaults are re-applied at compute time via
    /// <see cref="ParameterValues.WithDefaults"/>. Returns empty when the schema is null.
    /// </summary>
    public static Dictionary<string, object?> Capture(ParameterValues values, ModuleSchema? schema)
    {
        var result = new Dictionary<string, object?>();
        if (schema is null)
            return result;

        foreach (var p in schema.Parameters)
        {
            if (!values.Contains(p.Key))
                continue;
            var raw = values.GetRaw(p.Key);
            if (raw is null)
                continue;
            result[p.Key] = Normalize(raw, p.Kind);
        }
        return result;
    }

    /// <summary>
    /// Restores a normalized/JSON snapshot into a <see cref="ParameterValues"/> ready for compute.
    /// Unknown keys (params removed from the schema) are ignored; missing ones are left to defaults.
    /// Returns an empty bag when the schema is null.
    /// </summary>
    public static ParameterValues Restore(IReadOnlyDictionary<string, object?> raw, ModuleSchema? schema)
    {
        var dict = new Dictionary<string, object?>();
        if (schema is not null)
        {
            foreach (var p in schema.Parameters)
            {
                if (!raw.TryGetValue(p.Key, out var value) || value is null)
                    continue;
                dict[p.Key] = Normalize(value, p.Kind);
            }
        }
        return new ParameterValues(dict);
    }

    /// <summary>
    /// Coerces a single value — a boxed primitive or a <see cref="JsonElement"/> — to the CLR type
    /// its <paramref name="kind"/> requires.
    /// </summary>
    private static object? Normalize(object raw, ParameterKind kind) => kind switch
    {
        ParameterKind.Number => ToDouble(raw),
        ParameterKind.Integer => ToInt(raw),
        ParameterKind.Boolean => ToBool(raw),
        ParameterKind.Enum or ParameterKind.Text => ToStr(raw),
        ParameterKind.LayerStack => ToStringList(raw),
        _ => ToStr(raw),
    };

    private static double ToDouble(object raw) => raw switch
    {
        JsonElement e => e.ValueKind == JsonValueKind.String
            ? double.Parse(e.GetString()!, CultureInfo.InvariantCulture)
            : e.GetDouble(),
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        string s => double.Parse(s, CultureInfo.InvariantCulture),
        _ => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
    };

    private static int ToInt(object raw) => raw switch
    {
        JsonElement e => e.ValueKind == JsonValueKind.String
            ? int.Parse(e.GetString()!, CultureInfo.InvariantCulture)
            : (int)Math.Round(e.GetDouble()),
        int i => i,
        long l => checked((int)l),
        double d => (int)Math.Round(d),
        float f => (int)Math.Round(f),
        string s => int.Parse(s, CultureInfo.InvariantCulture),
        _ => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
    };

    private static bool ToBool(object raw) => raw switch
    {
        JsonElement e => e.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? e.GetBoolean()
            : bool.Parse(e.GetString()!),
        bool b => b,
        string s => bool.Parse(s),
        _ => Convert.ToBoolean(raw, CultureInfo.InvariantCulture),
    };

    private static string ToStr(object raw) => raw switch
    {
        JsonElement e => e.ValueKind == JsonValueKind.String
            ? e.GetString() ?? string.Empty
            : e.ToString(),
        string s => s,
        _ => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static List<string> ToStringList(object raw) => raw switch
    {
        JsonElement e when e.ValueKind == JsonValueKind.Array =>
            e.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList(),
        JsonElement e when e.ValueKind == JsonValueKind.String =>
            SplitCsv(e.GetString()),
        IEnumerable<string> list => list.ToList(),
        string s => SplitCsv(s),
        System.Collections.IEnumerable en =>
            en.Cast<object?>().Select(o => Convert.ToString(o, CultureInfo.InvariantCulture) ?? string.Empty).ToList(),
        _ => new List<string>(),
    };

    private static List<string> SplitCsv(string? s) =>
        string.IsNullOrEmpty(s)
            ? new List<string>()
            : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
