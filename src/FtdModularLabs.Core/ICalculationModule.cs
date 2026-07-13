namespace FtdModularLabs.Core;

/// <summary>
/// The contract every calculator module implements. The shell discovers modules through DI,
/// renders a form from <see cref="InputSchema"/>, and runs <see cref="ComputeAsync"/>.
/// </summary>
public interface ICalculationModule
{
    /// <summary>Stable identifier, e.g. "demo.kinetic-energy".</summary>
    string Id { get; }

    string Name { get; }

    string Description { get; }

    ModuleSchema InputSchema { get; }

    Task<CalculationResult> ComputeAsync(ParameterValues inputs, CancellationToken ct);
}
