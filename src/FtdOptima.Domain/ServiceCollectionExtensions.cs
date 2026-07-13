using FtdOptima.Domain.Catalog;
using FtdOptima.Domain.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FtdOptima.Domain;

/// <summary>
/// Registers the design-management layer: the subsystem-type registry (which consumes the
/// discovered <c>ICalculationModule</c> calculators) and the JSON-file repositories. The host must
/// also register an <see cref="IAppDataPaths"/> so the repositories know where to persist.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFtdDesignManagement(this IServiceCollection services)
    {
        services.AddSingleton<ISubsystemTypeRegistry, SubsystemTypeRegistry>();
        services.AddSingleton<IVehicleDesignRepository, JsonFileVehicleDesignRepository>();
        services.AddSingleton<ITemplateRepository, JsonFileTemplateRepository>();
        return services;
    }
}
