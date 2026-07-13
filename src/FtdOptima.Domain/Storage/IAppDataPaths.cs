namespace FtdOptima.Domain.Storage;

/// <summary>
/// Supplies the root directory under which all persisted data lives. Abstracted so Domain stays
/// UI/host-free and unit-testable (tests point <see cref="Root"/> at a temp dir; the app points it
/// at the per-user app-data folder).
/// </summary>
public interface IAppDataPaths
{
    /// <summary>Absolute path to the app's data root. Created lazily by the repositories.</summary>
    string Root { get; }
}
