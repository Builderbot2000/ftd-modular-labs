namespace FtdModularLabs.Domain.Catalog;

/// <summary>
/// The static catalogue of every designable subsystem type, mirroring docs/subsystems.md.
/// Modeled on the <c>ArmorLayerLibrary</c> preset-library pattern (All / Find). Only types whose
/// subsystem has a calculator carry a non-null <see cref="SubsystemType.CalculatorModuleId"/>;
/// today those are APS and Armor. Adding a calculator later = set the id here and register the module.
/// Each entry is one distinct FtD module type — never lump different types into a single entry.
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
            "Advanced Projectile System — modular cartridge cannons. The most complex weapon system. Also covers CIWS close-in gun roles."),
        new("weapon.cram", "CRAM Cannon", SubsystemCategory.Weapons, null,
            "Simple arc-firing cannons with pentagonal packing."),
        new("weapon.missiles", "Missile Launcher", SubsystemCategory.Weapons, null,
            "Guided munitions: missiles, torpedoes, rockets, bombs, mines, depth charges, and interceptors."),
        new("weapon.lasers", "Laser", SubsystemCategory.Weapons, null,
            "Cavity/pump laser weapons (also drive LAMS defense)."),
        new("weapon.particle", "Particle Cannon", SubsystemCategory.Weapons, null,
            "Beam weapons with tetris-tuned nodes."),
        new("weapon.flamethrower", "Flamethrower", SubsystemCategory.Weapons, null,
            "Short-range incendiary area weapon."),
        new("weapon.plasma", "Plasma Cannon", SubsystemCategory.Weapons, null,
            "Short-range plasma area weapon."),
        new("weapon.melee-drill", "Drill", SubsystemCategory.Weapons, null,
            "Melee drilling weapon."),
        new("weapon.nuke", "Tactical Nuke", SubsystemCategory.Weapons, null,
            "Tactical nuclear warhead."),
        new("weapon.mass-driver", "Mass Driver", SubsystemCategory.Weapons, null,
            "High-velocity mass driver."),

        // ---- Propulsion ----
        new("prop.jet-custom", "Custom Jet Engine", SubsystemCategory.Propulsion, null,
            "Player-built jet engine from intakes, injectors, exhausts, and afterburners."),
        new("prop.thruster", "Thruster", SubsystemCategory.Propulsion, null,
            "Prebuilt directional thrusters, including ion thrusters."),
        new("prop.propeller", "Propeller", SubsystemCategory.Propulsion, null,
            "Standard propellers."),
        new("prop.propeller-huge", "Huge Propeller", SubsystemCategory.Propulsion, null,
            "Huge / large propellers."),
        new("prop.wheel", "Wheels & Tracks", SubsystemCategory.Propulsion, null,
            "Land propulsion."),
        new("prop.helicopter-blade", "Helicopter Blade", SubsystemCategory.Propulsion, null,
            "Helicopter blades for rotor lift."),
        new("prop.hydrofoil", "Hydrofoil", SubsystemCategory.Propulsion, null,
            "Lift-generating hydrofoils."),
        new("prop.sail", "Sail", SubsystemCategory.Propulsion, null,
            "Wind-driven sails."),
        new("prop.paddle", "Paddle", SubsystemCategory.Propulsion, null,
            "Paddle propulsion."),
        new("prop.fin", "Fin", SubsystemCategory.Propulsion, null,
            "Water / air control fins."),

        // ---- Power & Energy ----
        new("power.fuel-engine", "Fuel Engine", SubsystemCategory.Power, null,
            "Cheap, high power density; built from cylinders/injectors."),
        new("power.steam-engine", "Steam Engine", SubsystemCategory.Power, null,
            "Boilers, turbines, pistons; high efficiency or high output."),
        new("power.rtg", "RTG", SubsystemCategory.Power, null,
            "Radioisotope generators feeding Engine Power."),
        new("power.battery", "Battery", SubsystemCategory.Power, null,
            "Energy storage feeding Engine Power."),

        // ---- Defence Systems ----
        new("defence.shield", "Shield", SubsystemCategory.Defence, null,
            "Ring shields (armor-class boost) and planar/projector shields (deflection)."),
        new("defence.lams", "LAMS", SubsystemCategory.Defence, null,
            "Laser Anti-Munition System."),
        new("defence.softkill", "Jammer / Decoy", SubsystemCategory.Defence, null,
            "Chaff, flares, heat decoys, radar/sonar simulators, signal jammers, and smoke dispensers."),
        new("defence.armor", "Armor", SubsystemCategory.Defence, "armor.spec",
            "Armor plating (also the buoyancy subsystem)."),

        // ---- Detection / Sensors ----
        new("detect.sensor", "Sensor", SubsystemCategory.Detection, null,
            "All detection: radar, sonar, cameras/IR, laser rangefinders, snoopers, and munition warners (detectors and trackers)."),

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
