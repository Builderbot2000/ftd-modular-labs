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
        DateTimeOffset modifiedUtc = default)
    {
        Id = id;
        Name = name;
        VehicleClass = vehicleClass;
        Modules = modules is null ? new List<DesignModule>() : new List<DesignModule>(modules);
        CreatedUtc = createdUtc;
        ModifiedUtc = modifiedUtc;
    }

    public Guid Id { get; }

    /// <summary>User-facing design name.</summary>
    public string Name { get; set; }

    /// <summary>The vehicle class this design targets, e.g. "Ship", "Aircraft" (free text).</summary>
    public string VehicleClass { get; set; }

    /// <summary>The modules that make up this design, in display order.</summary>
    public List<DesignModule> Modules { get; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset ModifiedUtc { get; set; }
}
