using FtdModularLabs.Domain.Catalog;

namespace FtdModularLabs.Domain.Model;

/// <summary>
/// Creates designs and modules — blank, from a template (copy-on-create deep copy), or as a
/// snapshot back into a template. All copies get fresh ids and independent value dictionaries so
/// editing a created entity never mutates its source template.
/// </summary>
public static class DesignFactory
{
    public static VehicleDesign CreateBlank(string name, string vehicleClass)
    {
        var now = DateTimeOffset.UtcNow;
        return new VehicleDesign(Guid.NewGuid(), name, vehicleClass, modules: null, createdUtc: now, modifiedUtc: now);
    }

    public static VehicleDesign CreateFromTemplate(DesignTemplate template, string name)
    {
        var now = DateTimeOffset.UtcNow;
        var modules = template.Modules.Select(InstantiateModule);
        return new VehicleDesign(Guid.NewGuid(), name, template.VehicleClass, modules, now, now);
    }

    public static DesignModule CreateBlankModule(SubsystemType type, string name) =>
        new(Guid.NewGuid(), name, type.Id);

    public static DesignModule CreateModuleFromTemplate(ModuleTemplate template, string name) =>
        new(Guid.NewGuid(), name, template.SubsystemTypeId, CloneValues(template.Values));

    /// <summary>Snapshots a design into a new user template (deep copy, fresh template id).</summary>
    public static DesignTemplate SnapshotAsTemplate(VehicleDesign design, string templateName) => new(
        Guid.NewGuid().ToString("N"),
        templateName,
        design.VehicleClass,
        design.Modules.Select(SnapshotModule).ToList(),
        IsBuiltIn: false);

    /// <summary>Snapshots a single module into a new user template (deep copy, fresh template id).</summary>
    public static ModuleTemplate SnapshotAsTemplate(DesignModule module, string templateName) => new(
        Guid.NewGuid().ToString("N"),
        templateName,
        module.SubsystemTypeId,
        CloneValues(module.Values),
        IsBuiltIn: false);

    private static DesignModule InstantiateModule(ModuleTemplate template) =>
        new(Guid.NewGuid(), template.Name, template.SubsystemTypeId, CloneValues(template.Values));

    private static ModuleTemplate SnapshotModule(DesignModule module) => new(
        Guid.NewGuid().ToString("N"),
        module.Name,
        module.SubsystemTypeId,
        CloneValues(module.Values),
        IsBuiltIn: false);

    /// <summary>
    /// Deep-copies a value dictionary. List/enumerable values (e.g. a LayerStack) are cloned so the
    /// copy never aliases the source; primitives, strings, and JsonElement (a struct) are value-copied.
    /// </summary>
    private static Dictionary<string, object?> CloneValues(IReadOnlyDictionary<string, object?> values)
    {
        var clone = new Dictionary<string, object?>(values.Count);
        foreach (var (key, value) in values)
            clone[key] = CloneValue(value);
        return clone;
    }

    private static object? CloneValue(object? value) => value switch
    {
        null => null,
        string s => s,
        System.Text.Json.JsonElement e => e,
        IEnumerable<string> list => list.ToList(),
        System.Collections.IEnumerable en and not string =>
            en.Cast<object?>().ToList(),
        _ => value,
    };
}
