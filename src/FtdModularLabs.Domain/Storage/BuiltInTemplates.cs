using System.Reflection;
using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Serialization;

namespace FtdModularLabs.Domain.Storage;

/// <summary>
/// Loads the shipped, read-only built-in templates from embedded JSON resources
/// (Templates/designs/*.json and Templates/modules/*.json). Loaded once and cached; these are
/// always available regardless of the on-disk user library.
/// </summary>
public static class BuiltInTemplates
{
    private const string DesignMarker = ".Templates.designs.";
    private const string ModuleMarker = ".Templates.modules.";

    public static IReadOnlyList<DesignTemplate> Designs { get; } = LoadDesigns();

    public static IReadOnlyList<ModuleTemplate> Modules { get; } = LoadModules();

    private static IReadOnlyList<DesignTemplate> LoadDesigns() =>
        ReadResources(DesignMarker)
            .Select(json => DomainMapper.FromDto(DomainJson.Deserialize<DesignTemplateDto>(json)!, isBuiltIn: true))
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<ModuleTemplate> LoadModules() =>
        ReadResources(ModuleMarker)
            .Select(json => DomainMapper.FromDto(DomainJson.Deserialize<ModuleTemplateDto>(json)!, isBuiltIn: true))
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IEnumerable<string> ReadResources(string marker)
    {
        var assembly = typeof(BuiltInTemplates).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.Contains(marker, StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
                continue;
            using var reader = new StreamReader(stream);
            yield return reader.ReadToEnd();
        }
    }
}
