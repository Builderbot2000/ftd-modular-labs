namespace FtdOptima.Domain.Catalog;

/// <summary>
/// The static catalogue of every designable subsystem type, mirroring docs/subsystems.md.
/// Modeled on the <c>ArmorLayerLibrary</c> preset-library pattern (All / Find). Only types whose
/// subsystem has a calculator carry a non-null <see cref="SubsystemType.CalculatorModuleId"/>;
/// today that is APS alone. Adding a calculator later = set the id here and register the module.
/// </summary>
public static class SubsystemCatalog
{
    /// <summary>All subsystem types, grouped by category in the order docs/subsystems.md lists them.</summary>
    public static IReadOnlyList<SubsystemType> All { get; } =
    [
        // ---- Weapons ----
        // CalculatorModuleId is the stable ICalculationModule.Id of ApsShellModule ("aps.shell-optimizer").
        // Kept as a literal so Domain depends only on Core, never on a module library.
        new("weapon.aps", "APS Turret", SubsystemCategory.Weapons, "aps.shell-optimizer",
            "Advanced Projectile System — modular cartridge cannons. The most complex weapon system."),
        new("weapon.cram", "CRAM Cannon", SubsystemCategory.Weapons, null,
            "Simple arc-firing cannons with pentagonal packing."),
        new("weapon.missiles", "Missile Launcher", SubsystemCategory.Weapons, null,
            "Guided munitions: missiles, torpedoes, rockets, bombs, mines, depth charges, interceptors."),
        new("weapon.lasers", "Laser", SubsystemCategory.Weapons, null,
            "Cavity/pump laser weapons (also drive LAMS defense)."),
        new("weapon.particle", "Particle Cannon", SubsystemCategory.Weapons, null,
            "Beam weapons with tetris-tuned nodes."),
        new("weapon.flamethrower", "Flamethrower / Plasma", SubsystemCategory.Weapons, null,
            "Short-range area weapons."),
        new("weapon.melee", "Melee Weapon", SubsystemCategory.Weapons, null,
            "Drills, spinning blades, ramming / impact weapons."),
        new("weapon.special", "Special Weapon", SubsystemCategory.Weapons, null,
            "Tactical nukes, mass drivers."),

        // ---- Propulsion ----
        new("prop.jet", "Jet Engine / Thruster", SubsystemCategory.Propulsion, null,
            "Jet engines and thrusters, including ion thrusters."),
        new("prop.propeller", "Propeller", SubsystemCategory.Propulsion, null,
            "Propellers and huge/large propellers."),
        new("prop.wheel", "Wheels & Tracks", SubsystemCategory.Propulsion, null,
            "Land propulsion."),
        new("prop.dediblade", "Dediblade / Rotor", SubsystemCategory.Propulsion, null,
            "Helicopter blades and dediblades for rotor lift."),
        new("prop.hydrofoil", "Hydrofoil / Sail / Fin", SubsystemCategory.Propulsion, null,
            "Hydrofoils, sails, paddles, water/air fins."),
        new("prop.spinblock", "Spinblock / Piston", SubsystemCategory.Propulsion, null,
            "Mechanical actuation."),

        // ---- Power & Energy ----
        new("power.fuel-engine", "Fuel Engine", SubsystemCategory.Power, null,
            "Cheap, high power density; built from cylinders/injectors."),
        new("power.steam-engine", "Steam Engine", SubsystemCategory.Power, null,
            "Boilers, turbines, pistons; high efficiency or high output."),
        new("power.electric", "Electric Engine / RTG / Battery", SubsystemCategory.Power, null,
            "Energy storage and generation feeding Engine Power."),

        // ---- Defence Systems ----
        new("defence.shield", "Shield", SubsystemCategory.Defence, null,
            "Ring shields (armor-class boost) and planar/projector shields (deflection)."),
        new("defence.lams", "LAMS", SubsystemCategory.Defence, null,
            "Laser Anti-Munition System."),
        new("defence.ciws", "CIWS", SubsystemCategory.Defence, null,
            "Kinetic / flak / CRAM close-in guns."),
        new("defence.interceptor", "Interceptor", SubsystemCategory.Defence, null,
            "Missile and torpedo interceptors."),
        new("defence.decoy", "Softkill / Decoy", SubsystemCategory.Defence, null,
            "Chaff, flares, radar/sonar simulators, heat decoys."),
        new("defence.jammer", "Jammer / Smoke", SubsystemCategory.Defence, null,
            "Signal jammers and smoke dispensers."),
        new("defence.armor", "Armor", SubsystemCategory.Defence, null,
            "Armor plating (also the buoyancy subsystem)."),

        // ---- Detection / Sensors ----
        new("detect.radar", "Radar", SubsystemCategory.Detection, null,
            "Active and passive radar."),
        new("detect.sonar", "Sonar", SubsystemCategory.Detection, null,
            "Active and passive sonar."),
        new("detect.camera", "Camera / IR", SubsystemCategory.Detection, null,
            "Visual and IR cameras, retroreflection sensors."),
        new("detect.rangefinder", "Rangefinder / Snooper", SubsystemCategory.Detection, null,
            "Laser rangefinders, wireless snoopers, munition warners."),

        // ---- AI / Control ----
        new("ai.mainframe", "AI Mainframe", SubsystemCategory.AiControl, null,
            "AI mainframes and behavior cards (naval, air, missile-guidance, etc.)."),
        new("ai.breadboard", "Breadboard / ACB / PID", SubsystemCategory.AiControl, null,
            "Logic and control systems."),
        new("ai.storage", "Storage", SubsystemCategory.AiControl, null,
            "Material, fuel, ammo, and energy containers."),
    ];

    private static readonly Dictionary<string, SubsystemType> ById =
        All.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

    /// <summary>Looks up a subsystem type by its stable <see cref="SubsystemType.Id"/> (case-insensitive).</summary>
    public static SubsystemType? Find(string id) =>
        id is not null && ById.TryGetValue(id, out var type) ? type : null;

    /// <summary>Types grouped by category, in catalog order — the shape the type picker renders.</summary>
    public static IReadOnlyList<IGrouping<SubsystemCategory, SubsystemType>> ByCategory { get; } =
        All.GroupBy(t => t.Category).ToList();
}
