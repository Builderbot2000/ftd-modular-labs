namespace FtdModularLabs.Modules.Aps.Model;

/// <summary>
/// An ordered stack of <see cref="ArmorLayer"/>s (the target armor composition) plus the
/// penetration math. Faithful port of ApsCalc's <c>Scheme.cs</c> (AoKishuba/ApsCalcUI, MIT).
/// The structural AC-chaining, angle HP inflation, and the analytic minimum-penetrating-velocity
/// solve were verified verbatim against source and against the FtD wiki's armour rules.
/// </summary>
public sealed class Scheme
{
    /// <summary>Front-to-back list of layers. Clone presets before adding so per-scheme AC state is isolated.</summary>
    public List<ArmorLayer> LayerList { get; } = [];

    public Scheme() { }

    public Scheme(IEnumerable<ArmorLayer> layers)
    {
        foreach (var layer in layers)
        {
            LayerList.Add(layer.Clone());
        }
        if (LayerList.Count > 0)
        {
            CalculateLayerAC();
        }
    }

    /// <summary>
    /// Sets each layer's effective AC = its RawAC plus 0.2·RawAC of the structural layer directly
    /// behind it (the FtD "backing armour" bonus, one layer deep, not across air gaps). Last layer
    /// keeps its raw AC.
    /// </summary>
    public void CalculateLayerAC()
    {
        for (int i = 0; i < LayerList.Count - 1; i++)
        {
            ArmorLayer current = LayerList[i];
            ArmorLayer next = LayerList[i + 1];
            current.AC = next.GivesACBonus ? current.RawAC + next.ACBonus : current.RawAC;
        }
        LayerList[^1].AC = LayerList[^1].RawAC;
    }

    /// <summary>
    /// Kinetic damage required to punch through the whole stack at the given AP and impact angle.
    /// Each layer needs <c>effHP · max(1, AC/AP)</c>, where effHP is inflated by 1/|cos(angle)|
    /// (angle divisor 240 for sabot = 3/4 effective angle). The running slope angle resets at air gaps.
    /// </summary>
    public float GetRequiredKD(float ap, float impactAngle, bool shellIsSabotHead)
    {
        float requiredKD = 0f;
        if (LayerList.Count == 0)
        {
            return requiredKD;
        }

        float divisor = shellIsSabotHead ? 240f : 180f;
        float baseAngle = 0f;
        foreach (ArmorLayer layer in LayerList)
        {
            if (!layer.GivesACBonus)
            {
                baseAngle = layer.BaseAngle;
            }

            float hpMultiplier = MathF.Max(1f, layer.AC / ap);
            requiredKD += layer.HP / MathF.Abs(MathF.Cos((impactAngle + baseAngle) * MathF.PI / divisor)) * hpMultiplier;
        }
        return requiredKD;
    }

    /// <summary>Thump path (Hollow Point): angle-free, uses RawAC. KD needed to destroy the whole stack.</summary>
    public float GetRequiredThump(float ap)
    {
        float requiredTD = 0f;
        foreach (ArmorLayer layer in LayerList)
        {
            requiredTD += layer.HP * MathF.Max(1f, layer.RawAC / ap);
        }
        return requiredTD;
    }

    /// <summary>HP-weighted average effective AC across the stack.</summary>
    public float GetAverageAC()
    {
        if (LayerList.Count == 0)
        {
            return 0f;
        }

        float totalHp = 0f;
        float acTimesHp = 0f;
        foreach (ArmorLayer layer in LayerList)
        {
            acTimesHp += layer.AC * layer.HP;
            totalHp += layer.HP;
        }
        return totalHp > 0f ? acTimesHp / totalHp : 0f;
    }

    /// <summary>
    /// Minimum <see cref="Shell.Velocity"/> at which the shell penetrates this scheme. 0 for an empty
    /// scheme; +infinity if the shell can never pen (zero AP or KD coefficient). Requires the shell's
    /// overall modifiers to have been computed already.
    /// </summary>
    public float CalculateMinVelocityToPenetrate(Shell shell, float impactAngleFromPerpendicularDegrees)
    {
        if (LayerList.Count == 0)
        {
            return 0f;
        }

        float alpha = shell.OverallArmorPierceModifier * 0.0175f; // AP / V
        float beta = shell.GaugeMultiplier
                   * shell.EffectiveProjectileModuleCount
                   * shell.OverallKineticDamageModifier
                   * 0.16f
                   * Shell.ApsModifier; // KD / V
        if (shell.HeadModule != ApsModule.HollowPoint)
        {
            beta *= MathF.Pow(500f / MathF.Max(shell.Gauge, 100f), 0.15f);
        }

        if (alpha <= 0f || beta <= 0f)
        {
            return float.PositiveInfinity;
        }

        float angleDivisor = shell.HeadModule == ApsModule.SabotHead ? 240f : 180f;
        int layerCount = LayerList.Count;
        float[] hpArray = new float[layerCount];
        float[] acArray = new float[layerCount];
        float baseAngle = 0f;
        for (int i = 0; i < layerCount; i++)
        {
            ArmorLayer layer = LayerList[i];
            if (!layer.GivesACBonus)
            {
                baseAngle = layer.BaseAngle;
            }
            hpArray[i] = layer.HP / MathF.Abs(MathF.Cos((impactAngleFromPerpendicularDegrees + baseAngle) * MathF.PI / angleDivisor));
            acArray[i] = layer.AC;
        }
        float vKD = SolveMinPenVelocity(hpArray, acArray, alpha, beta);

        if (shell.HeadModule == ApsModule.HollowPoint)
        {
            for (int i = 0; i < layerCount; i++)
            {
                hpArray[i] = LayerList[i].HP;
                acArray[i] = LayerList[i].RawAC;
            }
            float vThump = SolveMinPenVelocity(hpArray, acArray, alpha, beta);
            return MathF.Min(vKD, vThump);
        }

        return vKD;
    }

    /// <summary>
    /// Solves <c>beta·V ≥ Σ effHP·max(1, AC/(alpha·V))</c> for the smallest V. Layers are sorted by AC
    /// ascending; between breakpoints <c>V_k = AC_k/alpha</c> the requirement is the quadratic
    /// <c>beta·V² − S·V − T/alpha = 0</c> with positive root returned when it lands in the interval.
    /// </summary>
    private static float SolveMinPenVelocity(float[] hpArray, float[] acArray, float alpha, float beta)
    {
        int layerCount = hpArray.Length;

        int[] idx = new int[layerCount];
        for (int i = 0; i < layerCount; i++)
        {
            idx[i] = i;
        }
        Array.Sort(idx, (a, b) => acArray[a].CompareTo(acArray[b]));

        float s = 0f;
        float t = 0f;
        for (int i = 0; i < layerCount; i++)
        {
            t += hpArray[i] * acArray[i];
        }

        for (int k = 0; k <= layerCount; k++)
        {
            float vCandidate = (s + MathF.Sqrt(s * s + 4f * beta * t / alpha)) / (2f * beta);

            float lo = k == 0 ? 0f : acArray[idx[k - 1]] / alpha;
            float hi = k == layerCount ? float.PositiveInfinity : acArray[idx[k]] / alpha;

            if (vCandidate >= lo && vCandidate <= hi)
            {
                return vCandidate;
            }

            if (k < layerCount)
            {
                int j = idx[k];
                s += hpArray[j];
                t -= hpArray[j] * acArray[j];
            }
        }

        return float.PositiveInfinity; // unreachable: monotonicity guarantees a match
    }
}
