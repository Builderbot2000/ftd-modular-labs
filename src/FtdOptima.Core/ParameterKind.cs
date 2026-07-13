namespace FtdOptima.Core;

/// <summary>
/// The kind of an input parameter. Drives how the shell renders an editor for it.
/// </summary>
public enum ParameterKind
{
    Number,
    Integer,
    Enum,
    Boolean,
    Text,

    /// <summary>
    /// An ordered, variable-length list of selections drawn from <see cref="ParameterDescriptor.Options"/>.
    /// The stored value is an <see cref="IEnumerable{String}"/> of chosen option keys (e.g. an armor
    /// layer stack). Rendered by a dedicated list editor rather than a single-value control.
    /// </summary>
    LayerStack,
}
