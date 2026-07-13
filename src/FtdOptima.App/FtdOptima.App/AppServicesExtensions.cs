using FtdOptima.App.Models;
using FtdOptima.Domain;
using FtdOptima.Domain.Storage;
using FtdOptima.Modules.Aps;
using FtdOptima.Modules.Demo;

namespace FtdOptima.App;

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
            .AddApsModule();

    /// <summary>Registers persistence + the design-management services.</summary>
    public static IServiceCollection AddFtdPersistence(this IServiceCollection services) =>
        services
            .AddSingleton<IAppDataPaths, LocalAppDataPaths>()
            .AddFtdDesignManagement();
}
