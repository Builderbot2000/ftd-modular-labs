namespace FtdModularLabs.Core;

/// <summary>
/// Describes a single input parameter of a module so the shell can render an editor
/// and validate a value without knowing anything about the module itself.
/// </summary>
public sealed record ParameterDescriptor(
    string Key,
    string Label,
    ParameterKind Kind,
    object? Default = null,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? Options = null,
    string? Unit = null,
    string? Help = null,
    string? ReferenceSubsystemTypeId = null);
