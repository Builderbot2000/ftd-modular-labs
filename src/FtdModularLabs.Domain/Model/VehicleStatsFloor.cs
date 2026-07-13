namespace FtdModularLabs.Domain.Model;

/// <summary>
/// The vehicle-level aggregate of every module's <see cref="ModuleContribution"/>. Each stat is the
/// sum of the same-named field across all modules that report it; if no module reports a stat, the
/// value is <c>null</c> — displayed as "—" rather than the misleading "0".
/// </summary>
public sealed record VehicleStatsFloor(
    double? Weight,
    double? CostFloor,
    double? Buoyancy,
    double? Lift,
    double? Volume,
    double? PowerOutput,
    double? PowerDraw)
{
    public static VehicleStatsFloor Empty { get; } = new(null, null, null, null, null, null, null);

    public static VehicleStatsFloor FromModules(IEnumerable<DesignModule> modules)
    {
        double? weight = null, cost = null, buoyancy = null, lift = null, volume = null, powerOut = null, powerDraw = null;

        foreach (var m in modules)
        {
            if (m.Contribution is not { } c)
                continue;
            Add(ref weight, c.Weight);
            Add(ref cost, c.CostFloor);
            Add(ref buoyancy, c.Buoyancy);
            Add(ref lift, c.Lift);
            Add(ref volume, c.Volume);
            Add(ref powerOut, c.PowerOutput);
            Add(ref powerDraw, c.PowerDraw);
        }

        return new VehicleStatsFloor(weight, cost, buoyancy, lift, volume, powerOut, powerDraw);
    }

    private static void Add(ref double? acc, double? contribution)
    {
        if (contribution is not { } value)
            return;
        acc = (acc ?? 0.0) + value;
    }
}
