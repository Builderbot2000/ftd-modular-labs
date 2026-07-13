namespace FtdOptima.Domain.Model;

/// <summary>
/// A named starting point for a <see cref="VehicleDesign"/> (e.g. the built-in "Ship"). Creating a
/// design from it deep-copies its modules into a fresh independent design (copy-on-create).
/// </summary>
/// <param name="TemplateId">Stable id (slug for built-ins, GUID string for user templates).</param>
/// <param name="Name">Template display name, e.g. "Ship".</param>
/// <param name="VehicleClass">The vehicle class new designs inherit.</param>
/// <param name="Modules">The module templates copied into new designs, in order.</param>
/// <param name="IsBuiltIn">True for shipped, read-only templates; false for user-saved ones.</param>
public sealed record DesignTemplate(
    string TemplateId,
    string Name,
    string VehicleClass,
    IReadOnlyList<ModuleTemplate> Modules,
    bool IsBuiltIn);
