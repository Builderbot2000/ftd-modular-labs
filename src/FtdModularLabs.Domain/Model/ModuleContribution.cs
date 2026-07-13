namespace FtdModularLabs.Domain.Model;

/// <summary>
/// The vehicle-level stats a single <see cref="DesignModule"/> contributes toward the design's
/// aggregate floor. Every field is nullable — <c>null</c> means "this module does not report that
/// stat" and is skipped by <see cref="VehicleStatsFloor"/>, distinct from a reported zero.
/// </summary>
public sealed record ModuleContribution(
    double? Weight = null,
    double? CostFloor = null,
    double? Buoyancy = null,
    double? Lift = null,
    double? Volume = null,
    double? PowerOutput = null,
    double? PowerDraw = null)
{
    public static ModuleContribution Empty { get; } = new();

    public bool IsEmpty =>
        Weight is null && CostFloor is null && Buoyancy is null && Lift is null &&
        Volume is null && PowerOutput is null && PowerDraw is null;

    /// <summary>Reserved keys a calculator's <c>CalculationResult.Summary</c> may set to feed the
    /// vehicle floor. Values must be convertible to <see cref="double"/> via <see cref="System.Convert"/>.</summary>
    public static class SummaryKeys
    {
        public const string Weight = "weight";
        public const string Cost = "cost";
        public const string Buoyancy = "buoyancy";
        public const string Lift = "lift";
        public const string Volume = "volume";
        public const string PowerOutput = "powerOutput";
        public const string PowerDraw = "powerDraw";
    }

    /// <summary>Extracts a contribution from a calculator's summary map by reading the reserved
    /// <see cref="SummaryKeys"/>. Missing or unparseable keys stay <c>null</c>.</summary>
    public static ModuleContribution FromSummary(IReadOnlyDictionary<string, object> summary) => new(
        Weight: TryReadDouble(summary, SummaryKeys.Weight),
        CostFloor: TryReadDouble(summary, SummaryKeys.Cost),
        Buoyancy: TryReadDouble(summary, SummaryKeys.Buoyancy),
        Lift: TryReadDouble(summary, SummaryKeys.Lift),
        Volume: TryReadDouble(summary, SummaryKeys.Volume),
        PowerOutput: TryReadDouble(summary, SummaryKeys.PowerOutput),
        PowerDraw: TryReadDouble(summary, SummaryKeys.PowerDraw));

    private static double? TryReadDouble(IReadOnlyDictionary<string, object> map, string key)
    {
        if (!map.TryGetValue(key, out var raw) || raw is null)
            return null;
        try
        {
            return Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
