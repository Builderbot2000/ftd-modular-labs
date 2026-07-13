using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Serialization;

namespace FtdModularLabs.Domain.Storage;

/// <summary>
/// File-backed <see cref="IVehicleDesignRepository"/>: one JSON document per design at
/// <c>{Root}/designs/{id}.json</c>. Desktop-first; single-user so no locking is needed. Writes are
/// atomic (temp file + move).
/// </summary>
public sealed class JsonFileVehicleDesignRepository : IVehicleDesignRepository
{
    private readonly IAppDataPaths _paths;

    public JsonFileVehicleDesignRepository(IAppDataPaths paths) => _paths = paths;

    private string DesignsDir => Path.Combine(_paths.Root, "designs");

    private string FilePath(Guid id) => Path.Combine(DesignsDir, id.ToString("N") + ".json");

    public async Task<IReadOnlyList<VehicleDesign>> GetAllAsync(CancellationToken ct = default)
    {
        var dir = DesignsDir;
        if (!Directory.Exists(dir))
            return Array.Empty<VehicleDesign>();

        var designs = new List<VehicleDesign>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            ct.ThrowIfCancellationRequested();
            var design = await ReadAsync(file, ct).ConfigureAwait(false);
            if (design is not null)
                designs.Add(design);
        }
        return designs.OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<VehicleDesign?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var file = FilePath(id);
        return File.Exists(file) ? await ReadAsync(file, ct).ConfigureAwait(false) : null;
    }

    public async Task SaveAsync(VehicleDesign design, CancellationToken ct = default)
    {
        Directory.CreateDirectory(DesignsDir);
        design.ModifiedUtc = DateTimeOffset.UtcNow;
        var json = DomainJson.Serialize(DomainMapper.ToDto(design));
        await AtomicWriteAsync(FilePath(design.Id), json, ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var file = FilePath(id);
        if (File.Exists(file))
            File.Delete(file);
        return Task.CompletedTask;
    }

    private static async Task<VehicleDesign?> ReadAsync(string file, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
        var dto = DomainJson.Deserialize<VehicleDesignDto>(json);
        return dto is null ? null : DomainMapper.FromDto(dto);
    }

    internal static async Task AtomicWriteAsync(string path, string content, CancellationToken ct)
    {
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, content, ct).ConfigureAwait(false);
        File.Move(tmp, path, overwrite: true);
    }
}
