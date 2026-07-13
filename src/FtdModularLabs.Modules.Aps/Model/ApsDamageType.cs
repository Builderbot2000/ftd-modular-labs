namespace FtdModularLabs.Modules.Aps.Model;

/// <summary>
/// The APS damage types the optimizer can target. Each maps to a distinct payload and scoring
/// formula in <see cref="Shell"/> (ported from ApsCalc's <c>Shell.cs</c>). All types still require
/// the shell to penetrate the target scheme to deliver damage.
/// </summary>
public enum ApsDamageType
{
    Kinetic,
    HE,
    Frag,
    EMP,
    HEAT,
    Incendiary,
    MD,
}
