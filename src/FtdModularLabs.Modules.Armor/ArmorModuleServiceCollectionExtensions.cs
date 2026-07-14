using FtdModularLabs.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Modules.Armor;

/// <summary>Registration entry point local to the Armor module.</summary>
public static class ArmorModuleServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ArmorModule"/> as an <see cref="ICalculationModule"/>. The module pulls
    /// in an optional <see cref="IBreachRater"/> from DI — the APS module registers one to enable
    /// the breach-rating output; when unregistered the armor compute skips that section.
    /// </summary>
    public static IServiceCollection AddArmorModule(this IServiceCollection services) =>
        services.AddSingleton<ICalculationModule>(sp => new ArmorModule(sp.GetService<IBreachRater>()));
}
