using System.Text.Json;

namespace FtdOptima.Domain.Serialization;

// JSON transport shapes. Parameter values persist as a map of JsonElement so the on-disk form is
// exact and schema-independent; ParameterValueSnapshot.Restore coerces them by kind at compute time.

public sealed record DesignModuleDto(
    Guid Id,
    string Name,
    string SubsystemTypeId,
    Dictionary<string, JsonElement> Values);

public sealed record VehicleDesignDto(
    Guid Id,
    string Name,
    string VehicleClass,
    List<DesignModuleDto> Modules,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc);

public sealed record ModuleTemplateDto(
    string TemplateId,
    string Name,
    string SubsystemTypeId,
    Dictionary<string, JsonElement> Values);

public sealed record DesignTemplateDto(
    string TemplateId,
    string Name,
    string VehicleClass,
    List<ModuleTemplateDto> Modules);
