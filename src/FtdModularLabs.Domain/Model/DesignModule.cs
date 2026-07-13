namespace FtdModularLabs.Domain.Model;

/// <summary>
/// A named module instance within a <see cref="VehicleDesign"/> — a leaf entity classed by a
/// subsystem type. Named "DesignModule" (not "Module") to avoid clashing with
/// <c>ICalculationModule</c> and the APS <c>ApsModule</c> shell component.
/// </summary>
public sealed class DesignModule
{
    public DesignModule(Guid id, string name, string subsystemTypeId, IDictionary<string, object?>? values = null)
    {
        Id = id;
        Name = name;
        SubsystemTypeId = subsystemTypeId;
        Values = values is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(values);
    }

    public Guid Id { get; }

    /// <summary>User-facing instance name, e.g. "Main Battery APS Turret".</summary>
    public string Name { get; set; }

    /// <summary>The <see cref="Catalog.SubsystemType.Id"/> this module is classed as.</summary>
    public string SubsystemTypeId { get; }

    /// <summary>
    /// The saved parameter-value snapshot, normalized to JSON-friendly primitives
    /// (double / int / bool / string, or <see cref="List{String}"/> for a LayerStack). Empty for a
    /// subsystem type without a calculator.
    /// </summary>
    public Dictionary<string, object?> Values { get; }
}
