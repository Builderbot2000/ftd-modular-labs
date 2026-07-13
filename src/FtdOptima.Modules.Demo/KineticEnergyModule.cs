using FtdOptima.Core;

namespace FtdOptima.Modules.Demo;

/// <summary>
/// A trivial demo module that proves the framework plumbing end-to-end. It exercises every
/// renderer path (Number, Enum) and returns both a scalar summary and a small table.
/// KE = ½·m·v².
/// </summary>
public sealed class KineticEnergyModule : ICalculationModule
{
    public const string ModuleId = "demo.kinetic-energy";

    private static readonly IReadOnlyList<string> Materials = new[] { "Steel", "Tungsten", "Lead" };

    public string Id => ModuleId;

    public string Name => "Demo: Muzzle Kinetic Energy";

    public string Description =>
        "Computes projectile kinetic energy (½·m·v²) — a stand-in that proves module discovery, " +
        "generic form rendering, compute, and result display all work end-to-end.";

    public ModuleSchema InputSchema { get; } = new(new[]
    {
        new ParameterDescriptor(
            Key: "mass",
            Label: "Projectile mass",
            Kind: ParameterKind.Number,
            Default: 10.0,
            Min: 0.0,
            Unit: "kg",
            Help: "Mass of the projectile."),
        new ParameterDescriptor(
            Key: "velocity",
            Label: "Muzzle velocity",
            Kind: ParameterKind.Number,
            Default: 1000.0,
            Min: 0.0,
            Unit: "m/s",
            Help: "Speed of the projectile at the muzzle."),
        new ParameterDescriptor(
            Key: "material",
            Label: "Material",
            Kind: ParameterKind.Enum,
            Default: "Steel",
            Options: Materials,
            Help: "Projectile material (echoed back; does not affect the KE result)."),
    });

    public Task<CalculationResult> ComputeAsync(ParameterValues inputs, CancellationToken ct)
    {
        var errors = inputs.Validate(InputSchema);
        if (errors.Count > 0)
            throw new ArgumentException("Invalid inputs: " + string.Join(" ", errors));

        var resolved = inputs.WithDefaults(InputSchema);
        var mass = resolved.GetDouble("mass");
        var velocity = resolved.GetDouble("velocity");
        var material = resolved.GetEnumOption("material");

        var ke = KineticEnergy(mass, velocity);

        var summary = new Dictionary<string, object>
        {
            ["Kinetic energy"] = $"{ke:N0} J",
            ["Material"] = material,
        };

        var table = new ResultTable(
            title: "KE vs. velocity",
            columns: new[] { "Velocity (m/s)", "Kinetic energy (J)" },
            rows: new IReadOnlyList<object>[]
            {
                new object[] { $"{velocity:N0}", $"{KineticEnergy(mass, velocity):N0}" },
                new object[] { $"{2 * velocity:N0}", $"{KineticEnergy(mass, 2 * velocity):N0}" },
                new object[] { $"{3 * velocity:N0}", $"{KineticEnergy(mass, 3 * velocity):N0}" },
            });

        var result = new CalculationResult(
            summary,
            tables: new[] { table },
            notes: "Demo module — the real APS optimizer port is a follow-up.");

        return Task.FromResult(result);
    }

    /// <summary>Kinetic energy in joules: ½·m·v².</summary>
    public static double KineticEnergy(double massKg, double velocityMps) =>
        0.5 * massKg * velocityMps * velocityMps;
}
