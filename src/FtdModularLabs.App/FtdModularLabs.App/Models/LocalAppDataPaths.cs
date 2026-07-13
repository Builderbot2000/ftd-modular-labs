using FtdModularLabs.Domain.Storage;

namespace FtdModularLabs.App.Models;

/// <summary>
/// Desktop <see cref="IAppDataPaths"/>: persists under the per-user local app-data folder,
/// e.g. <c>%LOCALAPPDATA%\FtdModularLabs</c> on Windows or <c>~/.local/share/FtdModularLabs</c> on Linux.
/// Named <c>LocalAppDataPaths</c> to avoid clashing with <c>Windows.Storage.AppDataPaths</c>.
/// </summary>
public sealed class LocalAppDataPaths : IAppDataPaths
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FtdModularLabs");
}
