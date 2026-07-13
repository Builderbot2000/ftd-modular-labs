namespace FtdModularLabs.Core;

/// <summary>
/// The full input schema of a module: an ordered list of parameter descriptors.
/// </summary>
public sealed class ModuleSchema
{
    public ModuleSchema(IReadOnlyList<ParameterDescriptor> parameters)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public IReadOnlyList<ParameterDescriptor> Parameters { get; }

    public ParameterDescriptor? Find(string key) =>
        Parameters.FirstOrDefault(p => p.Key == key);
}
