using FtdOptima.Domain.Model;

namespace FtdOptima.Domain.Storage;

/// <summary>
/// Store for design and module templates. Built-in templates (shipped as embedded resources) are
/// read-only and always present; user templates persist to disk. <c>GetAll*</c> merges both.
/// </summary>
public interface ITemplateRepository
{
    Task<IReadOnlyList<DesignTemplate>> GetDesignTemplatesAsync(CancellationToken ct = default);

    /// <summary>Module templates, optionally filtered to a single subsystem type.</summary>
    Task<IReadOnlyList<ModuleTemplate>> GetModuleTemplatesAsync(string? subsystemTypeId = null, CancellationToken ct = default);

    /// <summary>Saves a user template. Throws if the template is flagged built-in.</summary>
    Task SaveDesignTemplateAsync(DesignTemplate template, CancellationToken ct = default);

    Task SaveModuleTemplateAsync(ModuleTemplate template, CancellationToken ct = default);

    /// <summary>Deletes a user template by id. Built-in ids are rejected.</summary>
    Task DeleteDesignTemplateAsync(string templateId, CancellationToken ct = default);

    Task DeleteModuleTemplateAsync(string templateId, CancellationToken ct = default);
}
