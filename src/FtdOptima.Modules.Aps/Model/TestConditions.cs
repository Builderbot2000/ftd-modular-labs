namespace FtdOptima.Modules.Aps.Model;

/// <summary>
/// The engagement / accounting assumptions a shell is evaluated under. Defaults mirror the
/// documented setup behind AoKishuba's "APS Shell Configurations" spreadsheet (30-minute sustained
/// fire, single loader, recoil absorbers, 45° impact).
/// </summary>
public sealed class TestConditions
{
    /// <summary>Impact angle from perpendicular, in degrees (the spreadsheet uses 45°).</summary>
    public float ImpactAngle { get; init; } = 45f;

    /// <summary>Sustained-fire accounting window, seconds (30 minutes per the spreadsheet).</summary>
    public float TestIntervalSeconds { get; init; } = 1800f;

    /// <summary>Material storage per m³ of storage block.</summary>
    public float StoragePerVolume { get; init; } = 500f;

    /// <summary>Material storage per unit cost of storage block.</summary>
    public float StoragePerCost { get; init; } = 250f;

    /// <summary>Regular loader: extra clips beyond the loader itself (0 = the spreadsheet base case).</summary>
    public float RegularClipsPerLoader { get; init; } = 0f;

    /// <summary>Regular loader: material intakes feeding the loader.</summary>
    public float RegularInputsPerLoader { get; init; } = 1f;

    /// <summary>Whether the mount uses recoil absorbers (included in marginal cost per the spreadsheet).</summary>
    public bool GunUsesRecoilAbsorbers { get; init; } = true;

    /// <summary>Number of barrels (affects cooling and inaccuracy).</summary>
    public int BarrelCount { get; init; } = 1;

    // Rail engine parameters — only relevant when a shell draws rail power.
    public float EnginePpm { get; init; } = 5000f;
    public float EnginePpv { get; init; } = 1000f;
    public float EnginePpc { get; init; } = 2000f;
    public bool UsesSpecialFuel { get; init; } = false;
}
