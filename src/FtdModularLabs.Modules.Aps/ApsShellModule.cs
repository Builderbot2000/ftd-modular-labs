using FtdModularLabs.Core;
using FtdModularLabs.Modules.Aps.Model;

namespace FtdModularLabs.Modules.Aps;

/// <summary>
/// Recommends the best APS kinetic shell configuration for a user-defined target armor composition.
/// Ports AoKishuba's ApsCalc mechanics (MIT) — it builds the target armor stack from the armor
/// builder, searches gauge / head / base / body / casing configurations, and ranks those that
/// penetrate by DPS-per-cost, DPS-per-volume, or raw DPS.
/// </summary>
public sealed class ApsShellModule : ICalculationModule
{
    public const string ModuleId = "aps.shell-optimizer";

    private static readonly IReadOnlyList<string> OptimizeOptions = ["DPS per cost", "DPS per volume", "DPS"];
    private static readonly IReadOnlyList<string> DamageTypeOptions =
        ["Kinetic", "HE", "HEAT", "Frag", "EMP", "Incendiary", "MD"];

    public string Id => ModuleId;

    public string Name => "APS Shell Optimizer";

    public string Description =>
        "Given the armor composition you intend to attack, searches APS shell configurations and " +
        "recommends the most effective shell for the chosen damage type — the calculator replacement " +
        "for reading the APS Shell Configurations spreadsheet by hand.";

    public ModuleSchema InputSchema { get; } = new(
    [
        new ParameterDescriptor(
            Key: "targetArmor",
            Label: "Target armor (front → back)",
            Kind: ParameterKind.LayerStack,
            Options: ArmorLayerLibrary.Names,
            Help: "Build the target stack by selecting layers, outermost first. Use 'Air' for an air gap."),
        new ParameterDescriptor(
            Key: "damageType",
            Label: "Damage type",
            Kind: ParameterKind.Enum,
            Default: "Kinetic",
            Options: DamageTypeOptions,
            Help: "The shell payload to optimize. All types must still penetrate to deliver damage."),
        new ParameterDescriptor(
            Key: "optimizeFor",
            Label: "Optimize for",
            Kind: ParameterKind.Enum,
            Default: "DPS per cost",
            Options: OptimizeOptions,
            Help: "Ranking metric, matching ApsCalc's objective."),
        new ParameterDescriptor(
            Key: "minGauge", Label: "Min gauge", Kind: ParameterKind.Number,
            Default: 100.0, Min: 18.0, Max: 500.0, Unit: "mm"),
        new ParameterDescriptor(
            Key: "maxGauge", Label: "Max gauge", Kind: ParameterKind.Number,
            Default: 500.0, Min: 18.0, Max: 500.0, Unit: "mm"),
        new ParameterDescriptor(
            Key: "maxLoaderLength", Label: "Max loader length", Kind: ParameterKind.Integer,
            Default: 4, Min: 1, Max: 8, Unit: "m",
            Help: "Longest shell the loader accepts (1–8 m)."),
        new ParameterDescriptor(
            Key: "impactAngle", Label: "Impact angle", Kind: ParameterKind.Number,
            Default: 45.0, Min: 0.0, Max: 80.0, Unit: "°",
            Help: "Angle from perpendicular. The spreadsheet uses 45°."),
        new ParameterDescriptor(
            Key: "allowRail", Label: "Allow railgun", Kind: ParameterKind.Boolean,
            Default: false, Help: "Include railgun casings + rail draw in the search."),
    ]);

    public Task<CalculationResult> ComputeAsync(ParameterValues inputs, CancellationToken ct)
    {
        var errors = inputs.Validate(InputSchema);
        if (errors.Count > 0)
        {
            throw new ArgumentException("Invalid inputs: " + string.Join(" ", errors));
        }

        var resolved = inputs.WithDefaults(InputSchema);

        var layerNames = resolved.GetStringList("targetArmor");
        var layers = layerNames
            .Select(ArmorLayerLibrary.Find)
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();
        var scheme = new Scheme(layers);

        var conditions = new TestConditions { ImpactAngle = (float)resolved.GetDouble("impactAngle") };

        var damageType = resolved.GetEnumOption("damageType") switch
        {
            "HE" => ApsDamageType.HE,
            "HEAT" => ApsDamageType.HEAT,
            "Frag" => ApsDamageType.Frag,
            "EMP" => ApsDamageType.EMP,
            "Incendiary" => ApsDamageType.Incendiary,
            "MD" => ApsDamageType.MD,
            _ => ApsDamageType.Kinetic,
        };

        var search = new SearchParameters
        {
            DamageType = damageType,
            MinGauge = (float)resolved.GetDouble("minGauge"),
            MaxGauge = (float)resolved.GetDouble("maxGauge"),
            MaxLoaderLengthMeters = resolved.GetInt("maxLoaderLength"),
            AllowRail = resolved.GetBool("allowRail"),
            Target = resolved.GetEnumOption("optimizeFor") switch
            {
                "DPS per volume" => OptimizationTarget.DpsPerVolume,
                "DPS" => OptimizationTarget.Dps,
                _ => OptimizationTarget.DpsPerCost,
            },
        };

        var top = ShellCalc.Search(scheme, conditions, search);

        if (top.Count == 0)
        {
            var empty = new Dictionary<string, object>
            {
                ["Result"] = layers.Count == 0
                    ? "Add at least one armor layer to the target."
                    : "No configuration in range penetrates this armor — widen the gauge/loader limits or allow railguns.",
            };
            return Task.FromResult(new CalculationResult(empty,
                notes: "APS kinetic optimizer — no penetrating configuration found."));
        }

        var best = top[0];

        // Per-turret material cost / volume floor — sum every subsystem the shell config needs.
        // The shell computes these in absolute (per-loader) units, so they are a valid contribution
        // to the vehicle-level VehicleStatsFloor.
        var turretCost =
            best.Shell.LoaderCost + best.Shell.RecoilCost + best.Shell.CoolerCost +
            best.Shell.ChargerCost + best.Shell.EngineCost +
            best.Shell.FuelAccessCost + best.Shell.FuelStorageCost +
            best.Shell.AmmoAccessCost + best.Shell.AmmoStorageCost;
        var turretVolume =
            best.Shell.LoaderVolume + best.Shell.RecoilVolume + best.Shell.CoolerVolume +
            best.Shell.ChargerVolume + best.Shell.EngineVolume +
            best.Shell.FuelAccessVolume + best.Shell.FuelStorageVolume +
            best.Shell.AmmoAccessVolume + best.Shell.AmmoStorageVolume;

        var summary = new Dictionary<string, object>
        {
            ["Recommended shell"] = best.Config,
            [$"{damageType} DPS"] = $"{best.Shell.Dps:N1}",
            ["DPS / cost"] = $"{best.Shell.DpsPerCost:N4}",
            ["DPS / volume"] = $"{best.Shell.DpsPerVolume:N3}",
            ["Muzzle velocity"] = $"{best.Shell.Velocity:N0} m/s",
            ["Armor pierce (AP)"] = $"{best.Shell.ArmorPierce:N1}",
            ["Target front AC"] = scheme.LayerList.Count > 0 ? $"{scheme.LayerList[0].AC:N1}" : "—",
            // Reserved keys read by ModuleContribution.FromSummary (Domain) to feed the vehicle
            // stats floor. Key strings must match ModuleContribution.SummaryKeys.
            ["cost"] = (double)turretCost,
            ["volume"] = (double)turretVolume,
        };

        var table = new ResultTable(
            title: $"Top {top.Count} configurations",
            columns: ["Configuration", "DPS", "DPS/cost", "DPS/vol", "Velocity (m/s)", "AP", "Reload (s)"],
            rows: top.Select(r => (IReadOnlyList<object>)new object[]
            {
                r.Config,
                $"{r.Shell.Dps:N1}",
                $"{r.Shell.DpsPerCost:N4}",
                $"{r.Shell.DpsPerVolume:N3}",
                $"{r.Shell.Velocity:N0}",
                $"{r.Shell.ArmorPierce:N1}",
                $"{r.Shell.ClusterReloadTime:N2}",
            }).ToList());

        var notes = $"Searched kinetic configs against a {scheme.LayerList.Count}-layer scheme " +
            $"(front AC {(scheme.LayerList.Count > 0 ? scheme.LayerList[0].AC.ToString("N1") : "—")}). " +
            "Ported from ApsCalc (AoKishuba/ApsCalcUI, MIT); constants verified against the game.";

        return Task.FromResult(new CalculationResult(summary, [table], notes));
    }
}
