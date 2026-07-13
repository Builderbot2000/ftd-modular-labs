namespace FtdOptima.Modules.Aps.Model;

/// <summary>What the optimizer maximizes.</summary>
public enum OptimizationTarget
{
    DpsPerCost,
    DpsPerVolume,
    Dps,
}

/// <summary>Bounds and knobs for the config search.</summary>
public sealed class SearchParameters
{
    public ApsDamageType DamageType { get; init; } = ApsDamageType.Kinetic;

    public float MinGauge { get; init; } = 100f;
    public float MaxGauge { get; init; } = 500f;

    /// <summary>Max total shell length in metres (the spreadsheet's "loader size" axis, 1–8).</summary>
    public int MaxLoaderLengthMeters { get; init; } = 8;

    /// <summary>Candidate base modules (null = no base).</summary>
    public IReadOnlyList<ApsModule?> Bases { get; init; } =
    [
        null, ApsModule.BaseBleeder, ApsModule.Supercav,
    ];

    /// <summary>Allow railgun draw (adds RG casings + draw to the search).</summary>
    public bool AllowRail { get; init; } = false;

    public OptimizationTarget Target { get; init; } = OptimizationTarget.DpsPerCost;

    /// <summary>Number of gauge samples across [MinGauge, MaxGauge].</summary>
    public int GaugeSamples { get; init; } = 18;

    /// <summary>Max total casings tried.</summary>
    public int MaxCasings { get; init; } = 16;

    /// <summary>Cap on body-module count considered (keeps small-gauge/long-loader searches bounded).</summary>
    public int MaxBodyModules { get; init; } = 24;

    /// <summary>Safety ceiling on configurations evaluated per search.</summary>
    public int MaxEvaluations { get; init; } = 2_000_000;

    public int TopN { get; init; } = 10;
}

/// <summary>One evaluated, penetrating shell plus its score and a human-readable config.</summary>
public sealed record ShellResult(Shell Shell, string Config, float Score);

/// <summary>
/// Bounded brute-force optimizer over shell configurations, ranking survivors that penetrate the
/// target armor scheme by the chosen effectiveness metric of the chosen damage type. Faithful in
/// spirit to ApsCalc's <c>ShellCalc</c> search (it maximizes DPS-per-cost / DPS-per-volume of a
/// damage type), with an explicit, bounded grid in place of ApsCalc's LP + coarse-peak refinement.
/// </summary>
public static class ShellCalc
{
    /// <summary>Head candidates for a damage type.</summary>
    private static IReadOnlyList<ApsModule> HeadsFor(ApsDamageType dt) => dt switch
    {
        ApsDamageType.Kinetic => [ApsModule.HeavyHead, ApsModule.APHead, ApsModule.SabotHead, ApsModule.HollowPoint, ApsModule.SkimmerTip],
        ApsDamageType.HE => [ApsModule.HEHead, ApsModule.APHead],
        ApsDamageType.HEAT => [ApsModule.ShapedChargeHead],
        ApsDamageType.Frag => [ApsModule.FragHead, ApsModule.APHead],
        ApsDamageType.EMP => [ApsModule.EmpHead, ApsModule.APHead],
        ApsDamageType.Incendiary => [ApsModule.IncendiaryHead, ApsModule.APHead],
        ApsDamageType.MD => [ApsModule.MDHead],
        _ => [ApsModule.APHead],
    };

    // The two body-module roles the search splits between: a "pen" body (helps punch through)
    // and a "payload" body (delivers the damage). For kinetic, sabot is the payload role.
    private static int PayloadBodyIndex(ApsDamageType dt) => dt switch
    {
        ApsDamageType.Kinetic => ApsModule.SabotBodyIndex,
        ApsDamageType.HE or ApsDamageType.HEAT => ApsModule.HEBodyIndex,
        ApsDamageType.Frag => ApsModule.FragBodyIndex,
        ApsDamageType.EMP => ApsModule.EmpBodyIndex,
        ApsDamageType.Incendiary => ApsModule.IncendiaryBodyIndex,
        ApsDamageType.MD => ApsModule.MDBodyIndex,
        _ => ApsModule.SolidBodyIndex,
    };

    public static IReadOnlyList<ShellResult> Search(Scheme scheme, TestConditions conditions, SearchParameters p)
    {
        var results = new List<ShellResult>();
        int bodyCount = ApsModule.BodyModules.Length;
        int penIdx = ApsModule.SolidBodyIndex;
        int payloadIdx = PayloadBodyIndex(p.DamageType);
        var heads = HeadsFor(p.DamageType);
        float lengthBudget = p.MaxLoaderLengthMeters * 1000f;
        int evaluations = 0;

        foreach (float gauge in SampleGauges(p))
        {
            float headLen = gauge;
            int maxBodyByLength = Math.Max(1, (int)MathF.Floor((lengthBudget - headLen) / gauge));
            int maxBodyModules = Math.Min(p.MaxBodyModules, maxBodyByLength);

            foreach (ApsModule head in heads)
            {
                foreach (ApsModule? baseMod in p.Bases)
                {
                    float baseLen = baseMod != null ? MathF.Min(gauge, baseMod.MaxLength) : 0f;

                    for (int totalBody = 1; totalBody <= maxBodyModules; totalBody++)
                    {
                        float projectileLen = baseLen + totalBody * gauge + headLen;
                        float casingRoom = lengthBudget - projectileLen;
                        if (casingRoom < 0f)
                        {
                            break;
                        }
                        int maxCasings = Math.Min(p.MaxCasings, (int)MathF.Floor(casingRoom / gauge));
                        if (maxCasings < 1)
                        {
                            continue;
                        }

                        foreach (int payload in PayloadSplits(totalBody, p.DamageType))
                        {
                            int pen = totalBody - payload;
                            EvaluateCasingSweep(scheme, conditions, p, results, gauge, head, baseMod,
                                bodyCount, penIdx, payloadIdx, pen, payload, maxCasings, ref evaluations);
                            if (evaluations >= p.MaxEvaluations)
                            {
                                return Rank(results, p);
                            }
                        }
                    }
                }
            }
        }

        return Rank(results, p);
    }

    private static IReadOnlyList<ShellResult> Rank(List<ShellResult> results, SearchParameters p) =>
        results.OrderByDescending(r => r.Score).Take(p.TopN).ToList();

    /// <summary>
    /// Splits worth trying between pen and payload bodies. Kinetic tries all-solid too (pure KD);
    /// chem types always keep some payload (all-solid delivers zero chem damage).
    /// </summary>
    private static IEnumerable<int> PayloadSplits(int totalBody, ApsDamageType dt)
    {
        if (dt == ApsDamageType.Kinetic)
        {
            yield return 0; // all solid
        }
        yield return totalBody; // all payload
        int half = totalBody / 2;
        if (half != 0 && half != totalBody)
        {
            yield return half; // mixed pen + payload
        }
    }

    private static void EvaluateCasingSweep(
        Scheme scheme, TestConditions conditions, SearchParameters p, List<ShellResult> results,
        float gauge, ApsModule head, ApsModule? baseMod, int bodyCount, int penIdx, int payloadIdx,
        int pen, int payload, int maxCasings, ref int evaluations)
    {
        for (int totalCasing = 1; totalCasing <= maxCasings; totalCasing++)
        {
            if (!p.AllowRail)
            {
                Evaluate(scheme, conditions, results, p, gauge, head, baseMod, bodyCount, penIdx, payloadIdx,
                    pen, payload, gpCasing: totalCasing, rgCasing: 0, railDrawFraction: 0f);
                evaluations++;
                continue;
            }

            foreach (float rgShare in RailCasingShares)
            {
                int rgCasing = (int)MathF.Round(totalCasing * rgShare);
                int gpCasing = totalCasing - rgCasing;
                foreach (float drawFrac in RailDrawFractions)
                {
                    Evaluate(scheme, conditions, results, p, gauge, head, baseMod, bodyCount, penIdx, payloadIdx,
                        pen, payload, gpCasing, rgCasing, rgCasing > 0 ? drawFrac : 0f);
                    evaluations++;
                }
            }
        }
    }

    private static readonly float[] RailCasingShares = [0f, 0.5f, 1f];
    private static readonly float[] RailDrawFractions = [0.5f, 1f];

    private static void Evaluate(
        Scheme scheme, TestConditions conditions, List<ShellResult> results, SearchParameters p,
        float gauge, ApsModule head, ApsModule? baseMod, int bodyCount, int penIdx, int payloadIdx,
        int pen, int payload, int gpCasing, int rgCasing, float railDrawFraction)
    {
        var body = new float[bodyCount];
        body[penIdx] += pen;
        body[payloadIdx] += payload;

        var shell = new Shell(gauge, head, baseMod, body, gpCasing, rgCasing, railDraw: 0f);

        if (railDrawFraction > 0f)
        {
            var probe = new Shell(gauge, head, baseMod, (float[])body.Clone(), gpCasing, rgCasing, 0f);
            probe.Evaluate(p.DamageType, scheme, conditions);
            shell.RailDraw = probe.MaxDrawShell * railDrawFraction;
        }

        shell.Evaluate(p.DamageType, scheme, conditions);
        if (!shell.Penetrates || shell.Dps <= 0f)
        {
            return;
        }

        float score = p.Target switch
        {
            OptimizationTarget.DpsPerVolume => shell.DpsPerVolume,
            OptimizationTarget.Dps => shell.Dps,
            _ => shell.DpsPerCost,
        };
        if (score <= 0f || float.IsNaN(score) || float.IsInfinity(score))
        {
            return;
        }

        results.Add(new ShellResult(shell, DescribeConfig(shell, p.DamageType, pen, payload), score));
    }

    private static float[] SampleGauges(SearchParameters p)
    {
        if (p.GaugeSamples <= 1 || p.MaxGauge <= p.MinGauge)
        {
            return [p.MinGauge];
        }
        var gauges = new float[p.GaugeSamples];
        float step = (p.MaxGauge - p.MinGauge) / (p.GaugeSamples - 1);
        for (int i = 0; i < p.GaugeSamples; i++)
        {
            gauges[i] = p.MinGauge + step * i;
        }
        return gauges;
    }

    private static string DescribeConfig(Shell shell, ApsDamageType dt, int pen, int payload)
    {
        var parts = new List<string> { $"{shell.Gauge:0} mm", shell.HeadModule.Name };
        if (shell.BaseModule != null)
        {
            parts.Add(shell.BaseModule.Name);
        }
        if (pen > 0)
        {
            parts.Add($"{pen}× solid");
        }
        if (payload > 0)
        {
            string payloadName = dt == ApsDamageType.Kinetic ? "sabot body" : $"{dt} body";
            parts.Add($"{payload}× {payloadName}");
        }
        if (shell.GPCasingCount > 0)
        {
            parts.Add($"{shell.GPCasingCount:0} GP");
        }
        if (shell.RGCasingCount > 0)
        {
            parts.Add($"{shell.RGCasingCount:0} RG @ {shell.RailDraw:0} draw");
        }
        return string.Join(", ", parts);
    }
}
