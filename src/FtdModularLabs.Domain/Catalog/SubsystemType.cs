namespace FtdModularLabs.Domain.Catalog;

/// <summary>
/// A kind of designable subsystem a module can be classed as (e.g. "APS Turret", "Steam Engine").
/// Types are code-defined and stable; a persisted <c>DesignModule</c> references one by <see cref="Id"/>.
/// </summary>
/// <param name="Id">Stable slug, e.g. "weapon.aps". Never reused for a different kind.</param>
/// <param name="Name">Human-facing name, e.g. "APS Turret".</param>
/// <param name="Category">The catalog section this type belongs to.</param>
/// <param name="CalculatorModuleId">
/// The <c>ICalculationModule.Id</c> of the calculator that optimizes this type, or <c>null</c> when no
/// calculator exists yet (the module is still nameable/persistable — its editor shows "coming soon").
/// </param>
/// <param name="Description">Short description of the subsystem, for the type picker.</param>
public sealed record SubsystemType(
    string Id,
    string Name,
    SubsystemCategory Category,
    string? CalculatorModuleId,
    string Description)
{
    /// <summary>True when a calculator is wired for this type.</summary>
    public bool HasCalculator => CalculatorModuleId is not null;
}
