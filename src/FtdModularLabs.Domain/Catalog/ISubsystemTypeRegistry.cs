using FtdModularLabs.Core;

namespace FtdModularLabs.Domain.Catalog;

/// <summary>
/// The single point that links persisted modules (which reference a subsystem type by id) to the
/// stateless <see cref="ICalculationModule"/> calculators discovered via DI. UI resolves a type's
/// calculator here; a null result means "calculator coming soon".
/// </summary>
public interface ISubsystemTypeRegistry
{
    /// <summary>Every subsystem type in the catalog.</summary>
    IReadOnlyList<SubsystemType> All { get; }

    /// <summary>Looks up a subsystem type by id.</summary>
    SubsystemType? FindType(string subsystemTypeId);

    /// <summary>
    /// Resolves the calculator for a subsystem type, or <c>null</c> when the type has no calculator id
    /// or no matching module was discovered.
    /// </summary>
    ICalculationModule? GetCalculator(string subsystemTypeId);

    /// <summary>The input schema of a type's calculator, or <c>null</c> when it has none.</summary>
    ModuleSchema? GetSchema(string subsystemTypeId);
}
