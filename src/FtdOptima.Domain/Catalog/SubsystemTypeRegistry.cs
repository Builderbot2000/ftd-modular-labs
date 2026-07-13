using FtdOptima.Core;

namespace FtdOptima.Domain.Catalog;

/// <summary>
/// Default registry: cross-references <see cref="SubsystemCatalog.All"/> against the calculators
/// discovered via DI (<see cref="ICalculationModule"/> singletons), matching each type's
/// <see cref="SubsystemType.CalculatorModuleId"/> to a module's <see cref="ICalculationModule.Id"/>.
/// </summary>
public sealed class SubsystemTypeRegistry : ISubsystemTypeRegistry
{
    private readonly Dictionary<string, ICalculationModule> _calculatorsById;

    public SubsystemTypeRegistry(IEnumerable<ICalculationModule> calculators)
    {
        _calculatorsById = calculators.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SubsystemType> All => SubsystemCatalog.All;

    public SubsystemType? FindType(string subsystemTypeId) => SubsystemCatalog.Find(subsystemTypeId);

    public ICalculationModule? GetCalculator(string subsystemTypeId)
    {
        var type = SubsystemCatalog.Find(subsystemTypeId);
        if (type?.CalculatorModuleId is null)
            return null;
        return _calculatorsById.TryGetValue(type.CalculatorModuleId, out var module) ? module : null;
    }

    public ModuleSchema? GetSchema(string subsystemTypeId) => GetCalculator(subsystemTypeId)?.InputSchema;
}
