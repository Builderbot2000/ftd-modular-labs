namespace FtdModularLabs.Modules.Aps.Model;

/// <summary>
/// A single From The Depths armor layer. Faithful port of ApsCalc's <c>Layer.cs</c>
/// (AoKishuba/ApsCalcUI, MIT). Per-material Armour Class, HP and the 20%-of-AC structural
/// bonus were cross-verified against the official FtD wiki. Stats are for 4 m beams unless the
/// name says otherwise; slopes/wedges carry a <see cref="BaseAngle"/> and are non-structural.
/// </summary>
public sealed class ArmorLayer
{
    public ArmorLayer(string name, float hp, float ac, bool givesAcBonus, float baseAngle)
    {
        Name = name;
        HP = hp;
        RawAC = ac;
        ACBonus = 0.2f * RawAC;
        AC = ac; // effective AC defaults to unbonused; recomputed by Scheme.CalculateLayerAC
        GivesACBonus = givesAcBonus;
        BaseAngle = baseAngle;
    }

    public string Name { get; }
    public float HP { get; }
    public float RawAC { get; }

    /// <summary>AC this (structural) layer donates to the layer directly in front of it (= 0.2·RawAC).</summary>
    public float ACBonus { get; }

    /// <summary>Effective AC, including any structural bonus from the layer behind. Set by <see cref="Scheme"/>.</summary>
    public float AC { get; set; }

    /// <summary>True for structural blocks (donate an AC bonus forward). False = air gap / non-structural.</summary>
    public bool GivesACBonus { get; }

    /// <summary>Impact-angle offset from perpendicular, in degrees (sloping geometry).</summary>
    public float BaseAngle { get; }

    /// <summary>A fresh mutable copy (AC is per-scheme state, so schemes must not share instances).</summary>
    public ArmorLayer Clone() => new(Name, HP, RawAC, GivesACBonus, BaseAngle);

    // ---- Preset layers (verbatim from Layer.cs) ----------------------------------------------
    public static ArmorLayer Air { get; } = new("Air", 0f, 0f, false, 0f);

    public static ArmorLayer AlloyBeam { get; } = new("Alloy 4m Beam", 1440f, 35f, true, 0f);
    public static ArmorLayer HeavyBeam { get; } = new("HA 4m Beam", 6000f, 60f, true, 0f);
    public static ArmorLayer MetalBeam { get; } = new("Metal 4m Beam", 1680f, 40f, true, 0f);
    public static ArmorLayer StoneBeam { get; } = new("Stone 4m Beam", 1200f, 16f, true, 0f);
    public static ArmorLayer WoodBeam { get; } = new("Wood 4m Beam", 960f, 8f, true, 0f);

    public static ArmorLayer AlloyBeamSlope { get; } = new("Alloy Beam Slope", 720f, 35f, false, 45f);
    public static ArmorLayer HeavyBeamSlope { get; } = new("HA Beam Slope", 3000f, 60f, false, 45f);
    public static ArmorLayer MetalBeamSlope { get; } = new("Metal Beam Slope", 840f, 40f, false, 45f);
    public static ArmorLayer StoneBeamSlope { get; } = new("Stone Beam Slope", 600f, 16f, false, 45f);
    public static ArmorLayer WoodBeamSlope { get; } = new("Wood Beam Slope", 480f, 8f, false, 45f);

    public static ArmorLayer Alloy2mSlopeSteep { get; } = new("Alloy 2m Slope (steep)", 330f, 35f, false, 26.56505f);
    public static ArmorLayer Heavy2mSlopeSteep { get; } = new("HA 2m Slope (steep)", 1375f, 60f, false, 26.56505f);
    public static ArmorLayer Metal2mSlopeSteep { get; } = new("Metal 2m Slope (steep)", 385f, 40f, false, 26.56505f);
    public static ArmorLayer Stone2mSlopeSteep { get; } = new("Stone 2m Slope (steep)", 275f, 16f, false, 26.56505f);
    public static ArmorLayer Wood2mSlopeSteep { get; } = new("Wood 2m Slope (steep)", 220f, 8f, false, 26.56505f);

    public static ArmorLayer Alloy2mSlopeShallow { get; } = new("Alloy 2m Slope (shallow)", 330f, 35f, false, 63.43495f);
    public static ArmorLayer Heavy2mSlopeShallow { get; } = new("HA 2m Slope (shallow)", 1375f, 60f, false, 63.43495f);
    public static ArmorLayer Metal2mSlopeShallow { get; } = new("Metal 2m Slope (shallow)", 385f, 40f, false, 63.43495f);
    public static ArmorLayer Stone2mSlopeShallow { get; } = new("Stone 2m Slope (shallow)", 275f, 16f, false, 63.43495f);
    public static ArmorLayer Wood2mSlopeShallow { get; } = new("Wood 2m Slope (shallow)", 220f, 8f, false, 63.43495f);

    public static ArmorLayer Alloy3mSlopeSteep { get; } = new("Alloy 3m Slope (steep)", 517.5f, 35f, false, 18.43495f);
    public static ArmorLayer Heavy3mSlopeSteep { get; } = new("HA 3m Slope (steep)", 2156.2f, 60f, false, 18.43495f);
    public static ArmorLayer Metal3mSlopeSteep { get; } = new("Metal 3m Slope (steep)", 603.8f, 40f, false, 18.43495f);
    public static ArmorLayer Stone3mSlopeSteep { get; } = new("Stone 3m Slope (steep)", 431.2f, 16f, false, 18.43495f);
    public static ArmorLayer Wood3mSlopeSteep { get; } = new("Wood 3m Slope (steep)", 345f, 8f, false, 18.43495f);

    public static ArmorLayer Alloy3mSlopeShallow { get; } = new("Alloy 3m Slope (shallow)", 517.5f, 35f, false, 71.56505f);
    public static ArmorLayer Heavy3mSlopeShallow { get; } = new("HA 3m Slope (shallow)", 2156.2f, 60f, false, 71.56505f);
    public static ArmorLayer Metal3mSlopeShallow { get; } = new("Metal 3m Slope (shallow)", 603.8f, 40f, false, 71.56505f);
    public static ArmorLayer Stone3mSlopeShallow { get; } = new("Stone 3m Slope (shallow)", 431.2f, 16f, false, 71.56505f);
    public static ArmorLayer Wood3mSlopeShallow { get; } = new("Wood 3m Slope (shallow)", 345f, 8f, false, 71.56505f);

    public static ArmorLayer Alloy4mSlopeSteep { get; } = new("Alloy 4m Slope (steep)", 720f, 35f, false, 14.03624f);
    public static ArmorLayer Heavy4mSlopeSteep { get; } = new("HA 4m Slope (steep)", 3000f, 60f, false, 14.03624f);
    public static ArmorLayer Metal4mSlopeSteep { get; } = new("Metal 4m Slope (steep)", 840f, 40f, false, 14.03624f);
    public static ArmorLayer Stone4mSlopeSteep { get; } = new("Stone 4m Slope (steep)", 600f, 16f, false, 14.03624f);
    public static ArmorLayer Wood4mSlopeSteep { get; } = new("Wood 4m Slope (steep)", 480f, 8f, false, 14.03624f);

    public static ArmorLayer Alloy4mSlopeShallow { get; } = new("Alloy 4m Slope (shallow)", 720f, 35f, false, 75.96376f);
    public static ArmorLayer Heavy4mSlopeShallow { get; } = new("HA 4m Slope (shallow)", 3000f, 60f, false, 75.96376f);
    public static ArmorLayer Metal4mSlopeShallow { get; } = new("Metal 4m Slope (shallow)", 840f, 40f, false, 75.96376f);
    public static ArmorLayer Stone4mSlopeShallow { get; } = new("Stone 4m Slope (shallow)", 600f, 16f, false, 75.96376f);
    public static ArmorLayer Wood4mSlopeShallow { get; } = new("Wood 4m Slope (shallow)", 480f, 8f, false, 75.96376f);

    public static ArmorLayer AlloyWedgeSteep { get; } = new("Alloy Wedge (steep)", 150f, 35f, false, 26.56505f);
    public static ArmorLayer HeavyWedgeSteep { get; } = new("HA Wedge (steep)", 625f, 60f, false, 26.56505f);
    public static ArmorLayer MetalWedgeSteep { get; } = new("Metal Wedge (steep)", 175f, 40f, false, 26.56505f);
    public static ArmorLayer StoneWedgeSteep { get; } = new("Stone Wedge (steep)", 125f, 16f, false, 26.56505f);
    public static ArmorLayer WoodWedgeSteep { get; } = new("Wood Wedge (steep)", 100f, 8f, false, 26.56505f);

    public static ArmorLayer AlloyWedgeShallow { get; } = new("Alloy Wedge (shallow)", 150f, 35f, false, 63.43495f);
    public static ArmorLayer HeavyWedgeShallow { get; } = new("HA Wedge (shallow)", 625f, 60f, false, 63.43495f);
    public static ArmorLayer MetalWedgeShallow { get; } = new("Metal Wedge (shallow)", 175f, 40f, false, 63.43495f);
    public static ArmorLayer StoneWedgeShallow { get; } = new("Stone Wedge (shallow)", 125f, 16f, false, 63.43495f);
    public static ArmorLayer WoodWedgeShallow { get; } = new("Wood Wedge (shallow)", 100f, 8f, false, 63.43495f);

    public static ArmorLayer Alloy2mWedgeSteep { get; } = new("Alloy 2m Wedge (steep)", 330f, 35f, false, 14.03624f);
    public static ArmorLayer Heavy2mWedgeSteep { get; } = new("HA 2m Wedge (steep)", 1375f, 60f, false, 14.03624f);
    public static ArmorLayer Metal2mWedgeSteep { get; } = new("Metal 2m Wedge (steep)", 385f, 40f, false, 14.03624f);
    public static ArmorLayer Stone2mWedgeSteep { get; } = new("Stone 2m Wedge (steep)", 275f, 16f, false, 14.03624f);
    public static ArmorLayer Wood2mWedgeSteep { get; } = new("Wood 2m Wedge (steep)", 220f, 8f, false, 14.03624f);

    public static ArmorLayer Alloy2mWedgeShallow { get; } = new("Alloy 2m Wedge (shallow)", 330f, 35f, false, 75.96376f);
    public static ArmorLayer Heavy2mWedgeShallow { get; } = new("HA 2m Wedge (shallow)", 1375f, 60f, false, 75.96376f);
    public static ArmorLayer Metal2mWedgeShallow { get; } = new("Metal 2m Wedge (shallow)", 385f, 40f, false, 75.96376f);
    public static ArmorLayer Stone2mWedgeShallow { get; } = new("Stone 2m Wedge (shallow)", 275f, 16f, false, 75.96376f);
    public static ArmorLayer Wood2mWedgeShallow { get; } = new("Wood 2m Wedge (shallow)", 220f, 8f, false, 75.96376f);

    public static ArmorLayer Alloy3mWedgeSteep { get; } = new("Alloy 3m Wedge (steep)", 517.5f, 35f, false, 9.46232f);
    public static ArmorLayer Heavy3mWedgeSteep { get; } = new("HA 3m Wedge (steep)", 2156.2f, 60f, false, 9.46232f);
    public static ArmorLayer Metal3mWedgeSteep { get; } = new("Metal 3m Wedge (steep)", 603.8f, 40f, false, 9.46232f);
    public static ArmorLayer Stone3mWedgeSteep { get; } = new("Stone 3m Wedge (steep)", 431.2f, 16f, false, 9.46232f);
    public static ArmorLayer Wood3mWedgeSteep { get; } = new("Wood 3m Wedge (steep)", 345f, 8f, false, 9.46232f);

    public static ArmorLayer Alloy3mWedgeShallow { get; } = new("Alloy 3m Wedge (shallow)", 517.5f, 35f, false, 80.53768f);
    public static ArmorLayer Heavy3mWedgeShallow { get; } = new("HA 3m Wedge (shallow)", 2156.2f, 60f, false, 80.53768f);
    public static ArmorLayer Metal3mWedgeShallow { get; } = new("Metal 3m Wedge (shallow)", 603.8f, 40f, false, 80.53768f);
    public static ArmorLayer Stone3mWedgeShallow { get; } = new("Stone 3m Wedge (shallow)", 431.2f, 16f, false, 80.53768f);
    public static ArmorLayer Wood3mWedgeShallow { get; } = new("Wood 3m Wedge (shallow)", 345f, 8f, false, 80.53768f);

    public static ArmorLayer Alloy4mWedgeSteep { get; } = new("Alloy 4m Wedge (steep)", 720f, 35f, false, 7.12502f);
    public static ArmorLayer Heavy4mWedgeSteep { get; } = new("HA 4m Wedge (steep)", 3000f, 60f, false, 7.12502f);
    public static ArmorLayer Metal4mWedgeSteep { get; } = new("Metal 4m Wedge (steep)", 840f, 40f, false, 7.12502f);
    public static ArmorLayer Stone4mWedgeSteep { get; } = new("Stone 4m Wedge (steep)", 600f, 16f, false, 7.12502f);
    public static ArmorLayer Wood4mWedgeSteep { get; } = new("Wood 4m Wedge (steep)", 480f, 8f, false, 7.12502f);

    public static ArmorLayer Alloy4mWedgeShallow { get; } = new("Alloy 4m Wedge (shallow)", 720f, 35f, false, 82.87498f);
    public static ArmorLayer Heavy4mWedgeShallow { get; } = new("HA 4m Wedge (shallow)", 3000f, 60f, false, 82.87498f);
    public static ArmorLayer Metal4mWedgeShallow { get; } = new("Metal 4m Wedge (shallow)", 840f, 40f, false, 82.87498f);
    public static ArmorLayer Stone4mWedgeShallow { get; } = new("Stone 4m Wedge (shallow)", 600f, 16f, false, 82.87498f);
    public static ArmorLayer Wood4mWedgeShallow { get; } = new("Wood 4m Wedge (shallow)", 480f, 8f, false, 82.87498f);
}
