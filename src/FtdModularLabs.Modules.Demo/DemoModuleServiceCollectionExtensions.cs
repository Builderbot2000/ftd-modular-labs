using FtdModularLabs.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Modules.Demo;

/// <summary>
/// Registration entry point local to the Demo module, so the app can pull it in with one call.
/// </summary>
public static class DemoModuleServiceCollectionExtensions
{
    public static IServiceCollection AddDemoModule(this IServiceCollection services) =>
        services.AddCalculationModule<KineticEnergyModule>();
}
