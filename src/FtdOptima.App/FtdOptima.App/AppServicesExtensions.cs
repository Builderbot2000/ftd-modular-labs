using FtdOptima.Modules.Demo;

namespace FtdOptima.App;

/// <summary>
/// The app's composition root aggregates every module library's own registration call.
/// Adding a new module = one <c>AddXxxModule()</c> line here.
/// </summary>
public static class AppServicesExtensions
{
    public static IServiceCollection AddFtdModules(this IServiceCollection services) =>
        services.AddDemoModule();
}
