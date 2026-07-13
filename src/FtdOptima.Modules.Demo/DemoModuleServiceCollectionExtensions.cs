using FtdOptima.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FtdOptima.Modules.Demo;

/// <summary>
/// Registration entry point local to the Demo module, so the app can pull it in with one call.
/// </summary>
public static class DemoModuleServiceCollectionExtensions
{
    public static IServiceCollection AddDemoModule(this IServiceCollection services) =>
        services.AddCalculationModule<KineticEnergyModule>();
}
