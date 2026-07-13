using System.Globalization;

namespace FtdOptima.Core;

/// <summary>
/// A bag of raw input values keyed by <see cref="ParameterDescriptor.Key"/>, with typed
/// getters and schema-aware validation. Values may arrive from UI controls as strings,
/// boxed numbers, bools, etc.; the getters coerce them to the requested CLR type.
/// </summary>
public sealed class ParameterValues
{
    private readonly Dictionary<string, object?> _values;

    public ParameterValues(IDictionary<string, object?>? values = null)
    {
        _values = values is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(values);
    }

    public bool Contains(string key) => _values.ContainsKey(key);

    public object? GetRaw(string key) => _values.TryGetValue(key, out var v) ? v : null;

    public ParameterValues Set(string key, object? value)
    {
        _values[key] = value;
        return this;
    }

    public double GetDouble(string key)
    {
        var raw = Require(key);
        return raw switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            string s => double.Parse(s, CultureInfo.InvariantCulture),
            _ => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
        };
    }

    public int GetInt(string key)
    {
        var raw = Require(key);
        return raw switch
        {
            int i => i,
            long l => checked((int)l),
            double d => checked((int)Math.Round(d)),
            float f => checked((int)Math.Round(f)),
            string s => int.Parse(s, CultureInfo.InvariantCulture),
            _ => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
        };
    }

    public bool GetBool(string key)
    {
        var raw = Require(key);
        return raw switch
        {
            bool b => b,
            string s => bool.Parse(s),
            _ => Convert.ToBoolean(raw, CultureInfo.InvariantCulture),
        };
    }

    public string GetString(string key) =>
        Require(key) as string ?? Convert.ToString(Require(key), CultureInfo.InvariantCulture) ?? string.Empty;

    public TEnum GetEnum<TEnum>(string key) where TEnum : struct, Enum
    {
        var raw = Require(key);
        return raw switch
        {
            TEnum e => e,
            string s => Enum.Parse<TEnum>(s, ignoreCase: true),
            _ => (TEnum)Enum.ToObject(typeof(TEnum), raw),
        };
    }

    /// <summary>Gets an enum-kind value as its raw string option.</summary>
    public string GetEnumOption(string key) => GetString(key);

    private object Require(string key)
    {
        if (!_values.TryGetValue(key, out var v) || v is null)
            throw new KeyNotFoundException($"No value supplied for parameter '{key}'.");
        return v;
    }

    /// <summary>
    /// Validates these values against <paramref name="schema"/>. Every required parameter
    /// (one without a default) must be present and coercible to its kind; numeric values
    /// must fall within any declared Min/Max; enum values must be one of the declared options.
    /// Missing values fall back to the descriptor's Default when one is provided.
    /// </summary>
    public IReadOnlyList<string> Validate(ModuleSchema schema)
    {
        var errors = new List<string>();

        foreach (var p in schema.Parameters)
        {
            var hasValue = _values.TryGetValue(p.Key, out var raw) && raw is not null;
            if (!hasValue)
            {
                if (p.Default is null)
                    errors.Add($"'{p.Label}' is required.");
                continue;
            }

            switch (p.Kind)
            {
                case ParameterKind.Number:
                case ParameterKind.Integer:
                    if (!TryCoerceDouble(raw!, out var num))
                    {
                        errors.Add($"'{p.Label}' must be a number.");
                        break;
                    }
                    if (p.Kind == ParameterKind.Integer && Math.Abs(num - Math.Round(num)) > double.Epsilon)
                        errors.Add($"'{p.Label}' must be a whole number.");
                    if (p.Min is { } min && num < min)
                        errors.Add($"'{p.Label}' must be >= {min}.");
                    if (p.Max is { } max && num > max)
                        errors.Add($"'{p.Label}' must be <= {max}.");
                    break;

                case ParameterKind.Enum:
                    var option = Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (p.Options is { Count: > 0 } && (option is null || !p.Options.Contains(option)))
                        errors.Add($"'{p.Label}' must be one of: {string.Join(", ", p.Options)}.");
                    break;

                case ParameterKind.Boolean:
                    if (raw is not bool && !(raw is string bs && bool.TryParse(bs, out _)))
                        errors.Add($"'{p.Label}' must be true or false.");
                    break;

                case ParameterKind.Text:
                    break;
            }
        }

        return errors;
    }

    /// <summary>
    /// Returns a copy of these values with any missing parameters filled in from schema defaults.
    /// </summary>
    public ParameterValues WithDefaults(ModuleSchema schema)
    {
        var merged = new Dictionary<string, object?>(_values);
        foreach (var p in schema.Parameters)
        {
            if ((!merged.TryGetValue(p.Key, out var v) || v is null) && p.Default is not null)
                merged[p.Key] = p.Default;
        }
        return new ParameterValues(merged);
    }

    private static bool TryCoerceDouble(object raw, out double value)
    {
        switch (raw)
        {
            case double d: value = d; return true;
            case float f: value = f; return true;
            case int i: value = i; return true;
            case long l: value = l; return true;
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p):
                value = p; return true;
            default:
                value = default;
                return false;
        }
    }
}
