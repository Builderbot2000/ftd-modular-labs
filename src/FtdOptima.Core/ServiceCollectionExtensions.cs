using Microsoft.Extensions.DependencyInjection;

namespace FtdOptima.Core;

/// <summary>
/// Composition-root helpers kept in Core so module libraries never take a UI/host dependency.
/// Each module library exposes its own <c>AddXxxModule()</c> (built on
/// <see cref="AddCalculationModule{TModule}"/>) so registration stays local to the module.
/// The app then aggregates them all in one <c>AddFtdModules()</c> call at its composition root.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a concrete module type as a singleton <see cref="ICalculationModule"/>.
    /// Module libraries call this from their own <c>AddXxxModule()</c> extension.
    /// </summary>
    public static IServiceCollection AddCalculationModule<TModule>(this IServiceCollection services)
        where TModule : class, ICalculationModule
    {
        return services.AddSingleton<ICalculationModule, TModule>();
    }
}
