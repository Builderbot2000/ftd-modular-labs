namespace FtdOptima.Domain.Model;

/// <summary>
/// A named starting point for a <see cref="DesignModule"/>. Creating a module from it deep-copies
/// the values into a fresh independent instance (copy-on-create). Built-in templates ship read-only.
/// </summary>
/// <param name="TemplateId">Stable id (slug for built-ins, GUID string for user templates).</param>
/// <param name="Name">Template display name, e.g. "APS Main Battery".</param>
/// <param name="SubsystemTypeId">The subsystem type a module created from this will be classed as.</param>
/// <param name="Values">The parameter-value snapshot to copy into new modules.</param>
/// <param name="IsBuiltIn">True for shipped, read-only templates; false for user-saved ones.</param>
public sealed record ModuleTemplate(
    string TemplateId,
    string Name,
    string SubsystemTypeId,
    IReadOnlyDictionary<string, object?> Values,
    bool IsBuiltIn);
