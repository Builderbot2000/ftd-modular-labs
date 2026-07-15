namespace FtdModularLabs.Modules.Armor.Model;

/// <summary>
/// An ordered stack of <see cref="ArmorLayer"/>s (the target armor composition) plus the
/// penetration math. Faithful port of ApsCalc's <c>Scheme.cs</c> (AoKishuba/ApsCalcUI, MIT).
/// The structural AC-chaining and angle HP inflation were verified verbatim against source
/// and against the FtD wiki's armour rules. The analytic minimum-penetrating-velocity solve
/// lives on <c>Shell</c> in the APS module (it needs shell-side modifiers).
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

    /// <summary>Total raw HP summed across every layer.</summary>
    public float GetTotalHp()
    {
        float total = 0f;
        foreach (ArmorLayer layer in LayerList)
        {
            total += layer.HP;
        }
        return total;
    }
}
