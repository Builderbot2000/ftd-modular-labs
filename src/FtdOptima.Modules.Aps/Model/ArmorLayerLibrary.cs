namespace FtdOptima.Modules.Aps.Model;

/// <summary>
/// The catalogue of selectable armor layers for the armor builder. Built from the 81 verbatim
/// <see cref="ArmorLayer"/> presets (not ApsCalc's <c>AllLayers</c> array, which duplicates the
/// Heavy 3 m slope entries and omits the Heavy 3 m wedges). Ordered by material then geometry.
/// </summary>
public static class ArmorLayerLibrary
{
    /// <summary>All selectable layers, in display order. "Air" is the air-gap / non-structural option.</summary>
    public static IReadOnlyList<ArmorLayer> All { get; } =
    [
        ArmorLayer.Air,

        ArmorLayer.HeavyBeam, ArmorLayer.HeavyBeamSlope,
        ArmorLayer.Heavy2mSlopeSteep, ArmorLayer.Heavy2mSlopeShallow,
        ArmorLayer.Heavy3mSlopeSteep, ArmorLayer.Heavy3mSlopeShallow,
        ArmorLayer.Heavy4mSlopeSteep, ArmorLayer.Heavy4mSlopeShallow,
        ArmorLayer.HeavyWedgeSteep, ArmorLayer.HeavyWedgeShallow,
        ArmorLayer.Heavy2mWedgeSteep, ArmorLayer.Heavy2mWedgeShallow,
        ArmorLayer.Heavy3mWedgeSteep, ArmorLayer.Heavy3mWedgeShallow,
        ArmorLayer.Heavy4mWedgeSteep, ArmorLayer.Heavy4mWedgeShallow,

        ArmorLayer.MetalBeam, ArmorLayer.MetalBeamSlope,
        ArmorLayer.Metal2mSlopeSteep, ArmorLayer.Metal2mSlopeShallow,
        ArmorLayer.Metal3mSlopeSteep, ArmorLayer.Metal3mSlopeShallow,
        ArmorLayer.Metal4mSlopeSteep, ArmorLayer.Metal4mSlopeShallow,
        ArmorLayer.MetalWedgeSteep, ArmorLayer.MetalWedgeShallow,
        ArmorLayer.Metal2mWedgeSteep, ArmorLayer.Metal2mWedgeShallow,
        ArmorLayer.Metal3mWedgeSteep, ArmorLayer.Metal3mWedgeShallow,
        ArmorLayer.Metal4mWedgeSteep, ArmorLayer.Metal4mWedgeShallow,

        ArmorLayer.AlloyBeam, ArmorLayer.AlloyBeamSlope,
        ArmorLayer.Alloy2mSlopeSteep, ArmorLayer.Alloy2mSlopeShallow,
        ArmorLayer.Alloy3mSlopeSteep, ArmorLayer.Alloy3mSlopeShallow,
        ArmorLayer.Alloy4mSlopeSteep, ArmorLayer.Alloy4mSlopeShallow,
        ArmorLayer.AlloyWedgeSteep, ArmorLayer.AlloyWedgeShallow,
        ArmorLayer.Alloy2mWedgeSteep, ArmorLayer.Alloy2mWedgeShallow,
        ArmorLayer.Alloy3mWedgeSteep, ArmorLayer.Alloy3mWedgeShallow,
        ArmorLayer.Alloy4mWedgeSteep, ArmorLayer.Alloy4mWedgeShallow,

        ArmorLayer.StoneBeam, ArmorLayer.StoneBeamSlope,
        ArmorLayer.Stone2mSlopeSteep, ArmorLayer.Stone2mSlopeShallow,
        ArmorLayer.Stone3mSlopeSteep, ArmorLayer.Stone3mSlopeShallow,
        ArmorLayer.Stone4mSlopeSteep, ArmorLayer.Stone4mSlopeShallow,
        ArmorLayer.StoneWedgeSteep, ArmorLayer.StoneWedgeShallow,
        ArmorLayer.Stone2mWedgeSteep, ArmorLayer.Stone2mWedgeShallow,
        ArmorLayer.Stone3mWedgeSteep, ArmorLayer.Stone3mWedgeShallow,
        ArmorLayer.Stone4mWedgeSteep, ArmorLayer.Stone4mWedgeShallow,

        ArmorLayer.WoodBeam, ArmorLayer.WoodBeamSlope,
        ArmorLayer.Wood2mSlopeSteep, ArmorLayer.Wood2mSlopeShallow,
        ArmorLayer.Wood3mSlopeSteep, ArmorLayer.Wood3mSlopeShallow,
        ArmorLayer.Wood4mSlopeSteep, ArmorLayer.Wood4mSlopeShallow,
        ArmorLayer.WoodWedgeSteep, ArmorLayer.WoodWedgeShallow,
        ArmorLayer.Wood2mWedgeSteep, ArmorLayer.Wood2mWedgeShallow,
        ArmorLayer.Wood3mWedgeSteep, ArmorLayer.Wood3mWedgeShallow,
        ArmorLayer.Wood4mWedgeSteep, ArmorLayer.Wood4mWedgeShallow,
    ];

    private static readonly Dictionary<string, ArmorLayer> ByName =
        All.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Layer names in display order — the option list for the armor-builder selector.</summary>
    public static IReadOnlyList<string> Names { get; } = All.Select(l => l.Name).ToList();

    /// <summary>Looks up a preset layer by its <see cref="ArmorLayer.Name"/> (case-insensitive).</summary>
    public static ArmorLayer? Find(string name) =>
        ByName.TryGetValue(name, out var layer) ? layer : null;
}
