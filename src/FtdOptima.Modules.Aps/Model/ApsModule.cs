namespace FtdOptima.Modules.Aps.Model;

/// <summary>
/// An APS shell module (head, body, base, or fuze) and its stat modifiers.
/// Faithful port of ApsCalc's <c>Module.cs</c> (AoKishuba/ApsCalcUI, MIT). Values verified
/// verbatim against that source and cross-checked against the From The Depths wiki.
/// </summary>
/// <remarks>
/// A shell is <c>Base + N·Middle(body) + Head</c>. Gunpowder/railgun casings are NOT modules —
/// they are tracked as float counts on <see cref="Shell"/>. Each module contributes length
/// <c>min(gauge, MaxLength)</c> mm and multiplicative Vel/KD/AP/Chem/Inacc modifiers.
/// </remarks>
public sealed class ApsModule
{
    public enum Position
    {
        Base,
        Middle,
        Head,
    }

    public ApsModule(
        string name,
        float velocityMod,
        float kineticDamageMod,
        float armorPierceMod,
        float chemMod,
        float inaccuracyMod,
        float maxLength,
        Position modulePosition,
        bool canBeVariable)
    {
        Name = name;
        VelocityMod = velocityMod;
        KineticDamageMod = kineticDamageMod;
        ArmorPierceMod = armorPierceMod;
        ChemMod = chemMod;
        InaccuracyMod = inaccuracyMod;
        MaxLength = maxLength;
        ModulePosition = modulePosition;
        CanBeVariable = canBeVariable;
    }

    public string Name { get; }
    public float VelocityMod { get; }
    public float KineticDamageMod { get; }
    public float ArmorPierceMod { get; }
    public float ChemMod { get; }
    public float InaccuracyMod { get; }

    /// <summary>Max module length in mm; a module's length equals the gauge at or below this value.</summary>
    public float MaxLength { get; }

    public Position ModulePosition { get; }

    /// <summary>Whether the optimizer may freely vary the count of this module.</summary>
    public bool CanBeVariable { get; }

    // ---- Middle (body) modules ---------------------------------------------------------------
    public static ApsModule SolidBody { get; } = new("Solid body", 1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule SabotBody { get; } = new("Sabot body", 1.1f, 0.8f, 1.4f, 0.25f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule EmpBody { get; } = new("EMP body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule MDBody { get; } = new("MD body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule FragBody { get; } = new("Frag body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule HEBody { get; } = new("HE body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule FinBody { get; } = new("Stabilizer fin body", 0.95f, 1.0f, 1.0f, 1.0f, 0.2f, 300f, Position.Middle, true);
    public static ApsModule SmokeBody { get; } = new("Smoke body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule IncendiaryBody { get; } = new("Incendiary body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
    public static ApsModule PenDepthFuse { get; } = new("Pendepth fuse", 1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
    public static ApsModule TimedFuse { get; } = new("Timed fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
    public static ApsModule InertialFuse { get; } = new("Inertial fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
    public static ApsModule AltitudeFuse { get; } = new("Altitude fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
    public static ApsModule Defuse { get; } = new("Emergency defuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
    public static ApsModule GravCompensator { get; } = new("Grav. compensator", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);

    // ---- Head modules ------------------------------------------------------------------------
    public static ApsModule EmpHead { get; } = new("EMP head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule MDHead { get; } = new("MD head", 1.45f, 1.0f, 0.1f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule FragHead { get; } = new("Frag head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule HEHead { get; } = new("HE head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule ShapedChargeHead { get; } = new("Shaped charge head", 1.6f, 0.1f, 0.1f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule APHead { get; } = new("AP head", 1.6f, 1.0f, 1.65f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule SabotHead { get; } = new("Sabot head", 1.6f, 0.85f, 2.5f, 0.25f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule HeavyHead { get; } = new("Heavy head", 1.45f, 1.75f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule HollowPoint { get; } = new("Hollow point head", 1.6f, 1.0f, 1.2f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule SkimmerTip { get; } = new("Skimmer tip", 1.6f, 1.0f, 1.4f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule Disruptor { get; } = new("Disruptor conduit", 1.6f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
    public static ApsModule IncendiaryHead { get; } = new("Incendiary head", 1.45f, 1.2f, 0.8f, 1.0f, 1.0f, 1000f, Position.Head, false);

    // ---- Base modules ------------------------------------------------------------------------
    public static ApsModule BaseBleeder { get; } = new("Base bleeder", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Base, false);
    public static ApsModule Supercav { get; } = new("Supercavitation base", 1.0f, 1.0f, 1.0f, 0.75f, 1.0f, 100f, Position.Base, false);
    public static ApsModule Tracer { get; } = new("Visible tracer", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Base, false);
    public static ApsModule GravRam { get; } = new("Graviton ram", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Base, false);

    /// <summary>
    /// All modules, in the canonical index order ApsCalc uses. <c>BodyModuleCounts</c> on
    /// <see cref="Shell"/> is indexed by the Middle-position subset in this order.
    /// </summary>
    public static ApsModule[] AllModules { get; } =
    [
        SolidBody, SabotBody, EmpBody, MDBody, FragBody, HEBody, FinBody, SmokeBody, IncendiaryBody,
        PenDepthFuse, TimedFuse, InertialFuse, AltitudeFuse, Defuse, GravCompensator,
        EmpHead, MDHead, FragHead, HEHead, ShapedChargeHead, APHead, SabotHead, HeavyHead,
        HollowPoint, SkimmerTip, Disruptor, IncendiaryHead,
        BaseBleeder, Supercav, Tracer, GravRam,
    ];

    /// <summary>The <see cref="Position.Middle"/> modules, in <see cref="AllModules"/> order.</summary>
    public static ApsModule[] BodyModules { get; } =
        Array.FindAll(AllModules, m => m.ModulePosition == Position.Middle);

    /// <summary>Index of a body module within <see cref="BodyModules"/> (and thus BodyModuleCounts).</summary>
    public static int BodyIndex(ApsModule m) => Array.IndexOf(BodyModules, m);

    public static int SolidBodyIndex { get; } = BodyIndex(SolidBody);
    public static int SabotBodyIndex { get; } = BodyIndex(SabotBody);
    public static int EmpBodyIndex { get; } = BodyIndex(EmpBody);
    public static int MDBodyIndex { get; } = BodyIndex(MDBody);
    public static int FragBodyIndex { get; } = BodyIndex(FragBody);
    public static int HEBodyIndex { get; } = BodyIndex(HEBody);
    public static int IncendiaryBodyIndex { get; } = BodyIndex(IncendiaryBody);
    public static int SmokeBodyIndex { get; } = BodyIndex(SmokeBody);
    public static int GravCompensatorIndex { get; } = BodyIndex(GravCompensator);
}
