using FtdOptima.Domain.Storage;

namespace FtdOptima.App.Models;

/// <summary>
/// Desktop <see cref="IAppDataPaths"/>: persists under the per-user local app-data folder,
/// e.g. <c>%LOCALAPPDATA%\FtdOptima</c> on Windows or <c>~/.local/share/FtdOptima</c> on Linux.
/// Named <c>LocalAppDataPaths</c> to avoid clashing with <c>Windows.Storage.AppDataPaths</c>.
/// </summary>
public sealed class LocalAppDataPaths : IAppDataPaths
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FtdOptima");
}
