using FtdModularLabs.Core;
using FtdModularLabs.Modules.Aps.Model;
using FtdModularLabs.Modules.Armor;
using FtdModularLabs.Modules.Armor.Model;

namespace FtdModularLabs.Modules.Aps;

/// <summary>
/// The APS-side implementation of <see cref="IBreachRater"/>: reads an <c>ApsShellModule</c>'s saved
/// values, runs the shell search against the given armor scheme, and reports the best-scoring
/// configuration plus its penetration verdict. Registered via
/// <c>AddApsModule</c> so the Armor module can display a breach rating without a direct
/// compile-time dependency on the APS project.
/// </summary>
public sealed class ApsBreachRater : IBreachRater
{
    public BreachRating Rate(Scheme scheme, ParameterValues apsValues, float impactAngle)
    {
        // The Armor module has resolved the referenced APS module's raw Values dictionary; feed it
        // through the APS input schema (defaults + normalization) so we work with the same shapes
        // the ApsShellModule expects.
        var apsResolved = apsValues.WithDefaults(new ApsShellModule().InputSchema);

        var conditions = new TestConditions { ImpactAngle = impactAngle };

        var damageType = apsResolved.GetEnumOption("damageType") switch
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
            MinGauge = (float)apsResolved.GetDouble("minGauge"),
            MaxGauge = (float)apsResolved.GetDouble("maxGauge"),
            MaxLoaderLengthMeters = apsResolved.GetInt("maxLoaderLength"),
            AllowRail = apsResolved.GetBool("allowRail"),
            Target = apsResolved.GetEnumOption("optimizeFor") switch
            {
                "DPS per volume" => OptimizationTarget.DpsPerVolume,
                "DPS" => OptimizationTarget.Dps,
                _ => OptimizationTarget.DpsPerCost,
            },
        };

        var top = ShellCalc.Search(scheme, conditions, search);
        if (top.Count == 0)
        {
            return new BreachRating(
                Verdict: "Not breached — the picked APS cannot penetrate this armor within its search bounds.",
                BestConfig: null, BestDps: 0f, BestAP: 0f, BestVelocity: 0f,
                MinPenVelocity: float.PositiveInfinity, Table: null,
                Notes: "The APS shell search found no penetrating configuration.");
        }

        var best = top[0];
        var breached = best.Shell.Penetrates && best.Shell.Dps > 0f;

        var table = new ResultTable(
            title: $"Top {top.Count} attacking configurations",
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

        var minPenV = best.Shell.MinVelocityToPenetrate(scheme, impactAngle);

        return new BreachRating(
            Verdict: breached ? "Breached." : "Not breached (best config lacks penetrating headroom).",
            BestConfig: best.Config,
            BestDps: best.Shell.Dps,
            BestAP: best.Shell.ArmorPierce,
            BestVelocity: best.Shell.Velocity,
            MinPenVelocity: minPenV,
            Table: table,
            Notes: $"Best APS configuration ranked by {search.Target}. " +
                   $"Impact angle {impactAngle:N0}°, damage type {damageType}.");
    }
}
