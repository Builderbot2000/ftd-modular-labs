namespace FtdModularLabs.Domain.Model;

/// <summary>
/// A persistent, user-named vehicle design (e.g. "Valiant-class Battleship") holding an ordered
/// collection of <see cref="DesignModule"/>s. The top-level CRUD entity of the app.
/// </summary>
public sealed class VehicleDesign
{
    public VehicleDesign(
        Guid id,
        string name,
        string vehicleClass,
        IEnumerable<DesignModule>? modules = null,
        DateTimeOffset createdUtc = default,
        DateTimeOffset modifiedUtc = default,
        string? description = null,
        double? manualCost = null)
    {
        Id = id;
        Name = name;
        VehicleClass = vehicleClass;
        Modules = modules is null ? new List<DesignModule>() : new List<DesignModule>(modules);
        CreatedUtc = createdUtc;
        ModifiedUtc = modifiedUtc;
        Description = description ?? string.Empty;
        ManualCost = manualCost;
    }

    public Guid Id { get; }

    /// <summary>User-facing design name.</summary>
    public string Name { get; set; }

    /// <summary>The vehicle class this design targets, e.g. "Ship", "Aircraft" (free text).</summary>
    public string VehicleClass { get; set; }

    /// <summary>Freeform notes about the design — role, tactics, in-game caveats. May be empty.</summary>
    public string Description { get; set; }

    /// <summary>The total material cost recorded by the player after building the design in-game.
    /// <c>null</c> when unset — distinct from an intentional zero.</summary>
    public double? ManualCost { get; set; }

    /// <summary>The modules that make up this design, in display order.</summary>
    public List<DesignModule> Modules { get; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset ModifiedUtc { get; set; }
}
