using FtdModularLabs.Domain.Model;

namespace FtdModularLabs.Domain.Storage;

/// <summary>CRUD store for <see cref="VehicleDesign"/> documents.</summary>
public interface IVehicleDesignRepository
{
    Task<IReadOnlyList<VehicleDesign>> GetAllAsync(CancellationToken ct = default);

    Task<VehicleDesign?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Inserts or updates a design, stamping <see cref="VehicleDesign.ModifiedUtc"/>.</summary>
    Task SaveAsync(VehicleDesign design, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
