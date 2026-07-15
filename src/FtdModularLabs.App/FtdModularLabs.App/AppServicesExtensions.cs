using FtdModularLabs.App.Models;
using FtdModularLabs.Domain;
using FtdModularLabs.Domain.Storage;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Armor;
using FtdModularLabs.Modules.Demo;

namespace FtdModularLabs.App;

/// <summary>
/// The app's composition root aggregates every module library's own registration call plus the
/// design-management layer (repositories, subsystem-type registry, app-data paths).
/// </summary>
public static class AppServicesExtensions
{
    /// <summary>Registers all FTD calculator modules for discovery by the subsystem-type registry.</summary>
    public static IServiceCollection AddFtdModules(this IServiceCollection services) =>
        services
            .AddDemoModule()
            .AddArmorModule()
            .AddApsModule();

    /// <summary>Registers persistence + the design-management services.</summary>
    public static IServiceCollection AddFtdPersistence(this IServiceCollection services) =>
        services
            .AddSingleton<IAppDataPaths, LocalAppDataPaths>()
            .AddFtdDesignManagement();
}
