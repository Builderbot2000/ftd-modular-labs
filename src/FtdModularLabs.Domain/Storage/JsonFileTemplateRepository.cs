using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Serialization;

namespace FtdModularLabs.Domain.Storage;

/// <summary>
/// File-backed <see cref="ITemplateRepository"/>. Built-in templates come from
/// <see cref="BuiltInTemplates"/> (embedded, read-only); user templates live under
/// <c>{Root}/templates/designs</c> and <c>{Root}/templates/modules</c>. Reads merge both, built-ins first.
/// </summary>
public sealed class JsonFileTemplateRepository : ITemplateRepository
{
    private readonly IAppDataPaths _paths;

    public JsonFileTemplateRepository(IAppDataPaths paths) => _paths = paths;

    private string DesignsDir => Path.Combine(_paths.Root, "templates", "designs");
    private string ModulesDir => Path.Combine(_paths.Root, "templates", "modules");

    public async Task<IReadOnlyList<DesignTemplate>> GetDesignTemplatesAsync(CancellationToken ct = default)
    {
        var user = await ReadAllAsync(DesignsDir,
            json => DomainMapper.FromDto(DomainJson.Deserialize<DesignTemplateDto>(json)!, isBuiltIn: false), ct)
            .ConfigureAwait(false);
        return BuiltInTemplates.Designs
            .Concat(user.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<ModuleTemplate>> GetModuleTemplatesAsync(string? subsystemTypeId = null, CancellationToken ct = default)
    {
        var user = await ReadAllAsync(ModulesDir,
            json => DomainMapper.FromDto(DomainJson.Deserialize<ModuleTemplateDto>(json)!, isBuiltIn: false), ct)
            .ConfigureAwait(false);
        var all = BuiltInTemplates.Modules
            .Concat(user.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase));
        if (subsystemTypeId is not null)
            all = all.Where(t => string.Equals(t.SubsystemTypeId, subsystemTypeId, StringComparison.OrdinalIgnoreCase));
        return all.ToList();
    }

    public async Task SaveDesignTemplateAsync(DesignTemplate template, CancellationToken ct = default)
    {
        RejectBuiltIn(template.IsBuiltIn);
        Directory.CreateDirectory(DesignsDir);
        var json = DomainJson.Serialize(DomainMapper.ToDto(template));
        await JsonFileVehicleDesignRepository.AtomicWriteAsync(UserFile(DesignsDir, template.TemplateId), json, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveModuleTemplateAsync(ModuleTemplate template, CancellationToken ct = default)
    {
        RejectBuiltIn(template.IsBuiltIn);
        Directory.CreateDirectory(ModulesDir);
        var json = DomainJson.Serialize(DomainMapper.ToDto(template));
        await JsonFileVehicleDesignRepository.AtomicWriteAsync(UserFile(ModulesDir, template.TemplateId), json, ct)
            .ConfigureAwait(false);
    }

    public Task DeleteDesignTemplateAsync(string templateId, CancellationToken ct = default) =>
        DeleteUserFileAsync(DesignsDir, templateId);

    public Task DeleteModuleTemplateAsync(string templateId, CancellationToken ct = default) =>
        DeleteUserFileAsync(ModulesDir, templateId);

    private Task DeleteUserFileAsync(string dir, string templateId)
    {
        RejectBuiltInId(templateId);
        var file = UserFile(dir, templateId);
        if (File.Exists(file))
            File.Delete(file);
        return Task.CompletedTask;
    }

    private static async Task<List<T>> ReadAllAsync<T>(string dir, Func<string, T> map, CancellationToken ct)
    {
        if (!Directory.Exists(dir))
            return new List<T>();
        var items = new List<T>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            ct.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            items.Add(map(json));
        }
        return items;
    }

    // Sanitize the template id into a safe file name (user ids are GUIDs, but stay defensive).
    private static string UserFile(string dir, string templateId)
    {
        var safe = string.Concat(templateId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(dir, safe + ".json");
    }

    private static void RejectBuiltIn(bool isBuiltIn)
    {
        if (isBuiltIn)
            throw new InvalidOperationException("Built-in templates are read-only and cannot be saved.");
    }

    private static void RejectBuiltInId(string templateId)
    {
        if (BuiltInTemplates.Designs.Any(t => t.TemplateId == templateId) ||
            BuiltInTemplates.Modules.Any(t => t.TemplateId == templateId))
            throw new InvalidOperationException("Built-in templates are read-only and cannot be deleted.");
    }
}
