using FtdModularLabs.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Modules.Aps;

/// <summary>Registration entry point local to the APS module.</summary>
public static class ApsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddApsModule(this IServiceCollection services) =>
        services.AddCalculationModule<ApsShellModule>();
}
