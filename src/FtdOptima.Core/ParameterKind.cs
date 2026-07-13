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
}
