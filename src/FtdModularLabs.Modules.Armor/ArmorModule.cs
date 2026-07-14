using FtdModularLabs.Core;
using FtdModularLabs.Modules.Armor.Model;

namespace FtdModularLabs.Modules.Armor;

/// <summary>
/// Characterizes an armor stack (layers, HP, effective AC) and — when the user picks an attacking
/// APS module in the same design — reports how well that APS breaches this armor at the chosen
/// impact angle. The armor stack lives here (one first-class module) instead of being duplicated
/// inline in every APS module that wants to attack it. The APS-side breach math is delegated to
/// an <see cref="IBreachRater"/> registered by the APS project, so this project has no compile-time
/// dependency on APS.
/// </summary>
public sealed class ArmorModule : ICalculationModule
{
    public const string ModuleId = "armor.spec";

    /// <summary>The subsystem type id an <c>attackingAps</c> reference must resolve to.</summary>
    public const string ApsSubsystemTypeId = "weapon.aps";

    private readonly IBreachRater? _rater;

    public ArmorModule() : this(null) { }

    public ArmorModule(IBreachRater? rater)
    {
        _rater = rater;
    }

    public string Id => ModuleId;

    public string Name => "Armor Spec";

    public string Description =>
        "Defines an armor stack (front → back) and, if an attacking APS module is selected, " +
        "rates how well that APS breaches this armor at the given impact angle.";

    public ModuleSchema InputSchema { get; } = new(
    [
        new ParameterDescriptor(
            Key: "targetArmor",
            Label: "Armor stack (front → back)",
            Kind: ParameterKind.LayerStack,
            Options: ArmorLayerLibrary.Names,
            Help: "Build the stack by selecting layers, outermost first. Use 'Air' for an air gap."),
        new ParameterDescriptor(
            Key: "attackingAps",
            Label: "Attacking APS (optional)",
            Kind: ParameterKind.ModuleReference,
            Default: "",
            ReferenceSubsystemTypeId: ApsSubsystemTypeId,
            Help: "Pick an APS turret in this design to rate its breach performance against this armor."),
        new ParameterDescriptor(
            Key: "impactAngle",
            Label: "Impact angle",
            Kind: ParameterKind.Number,
            Default: 45.0, Min: 0.0, Max: 80.0, Unit: "°",
            Help: "Angle from perpendicular used when rating the attacking APS."),
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

        if (layers.Count == 0)
        {
            var empty = new Dictionary<string, object>
            {
                ["Result"] = "Add at least one armor layer.",
            };
            return Task.FromResult(new CalculationResult(empty,
                notes: "Armor spec — build the layer stack front → back."));
        }

        var scheme = new Scheme(layers);

        var summary = new Dictionary<string, object>
        {
            ["Layer count"] = $"{scheme.LayerList.Count}",
            ["Total HP"] = $"{scheme.GetTotalHp():N0}",
            ["Avg AC (HP-weighted)"] = $"{scheme.GetAverageAC():N2}",
            ["Front layer AC"] = $"{scheme.LayerList[0].AC:N2}",
        };

        var layerRows = scheme.LayerList
            .Select((l, i) => (IReadOnlyList<object>)new object[]
            {
                $"{i + 1}",
                l.Name,
                $"{l.HP:N0}",
                $"{l.AC:N1}",
                l.GivesACBonus ? "structural" : "non-structural",
                $"{l.BaseAngle:N1}°",
            })
            .ToList();
        var layerTable = new ResultTable(
            title: "Layers (front → back)",
            columns: ["#", "Layer", "HP", "Eff. AC", "Kind", "Base angle"],
            rows: layerRows);

        var tables = new List<ResultTable> { layerTable };

        var notes = $"{scheme.LayerList.Count}-layer armor scheme, avg AC {scheme.GetAverageAC():N1}. " +
            "Pick an attacking APS to rate breach performance.";

        // When the module editor has resolved the attackingAps reference to a values dictionary,
        // rate it against this scheme using the APS-provided IBreachRater.
        var apsRaw = resolved.GetRaw("attackingAps");
        var apsRefResolved = apsRaw is IDictionary<string, object?>
                          || apsRaw is IReadOnlyDictionary<string, object?>
                          || apsRaw is ParameterValues;
        if (apsRefResolved)
        {
            var apsValues = resolved.GetReferencedValues("attackingAps");
            var breach = _rater?.Rate(scheme, apsValues, (float)resolved.GetDouble("impactAngle"));
            if (breach is null)
            {
                summary["Breach rating"] = "APS rating unavailable — no IBreachRater registered.";
            }
            else
            {
                summary["Breach verdict"] = breach.Verdict;
                if (breach.BestConfig is not null)
                {
                    summary["Best shell vs. this armor"] = breach.BestConfig;
                    summary["Best shell DPS"] = $"{breach.BestDps:N1}";
                    summary["Best shell AP"] = $"{breach.BestAP:N1}";
                    summary["Best shell velocity"] = $"{breach.BestVelocity:N0} m/s";
                    summary["Min pen velocity"] = float.IsInfinity(breach.MinPenVelocity)
                        ? "unreachable"
                        : $"{breach.MinPenVelocity:N0} m/s";
                }

                if (breach.Table is not null)
                {
                    tables.Add(breach.Table);
                }

                notes = breach.Notes ?? notes;
            }
        }

        return Task.FromResult(new CalculationResult(summary, tables, notes));
    }
}

/// <summary>Result of running the APS breach rating against a target armor scheme.</summary>
public sealed record BreachRating(
    string Verdict,
    string? BestConfig,
    float BestDps,
    float BestAP,
    float BestVelocity,
    float MinPenVelocity,
    ResultTable? Table,
    string? Notes);

/// <summary>
/// Cross-project seam: the APS project supplies an implementation and registers it with DI so the
/// Armor module can score its stack against a picked APS turret without a compile-time dependency
/// on APS. Optional — when unregistered, the armor module just skips the breach rating.
/// </summary>
public interface IBreachRater
{
    BreachRating Rate(Scheme scheme, ParameterValues apsValues, float impactAngle);
}
