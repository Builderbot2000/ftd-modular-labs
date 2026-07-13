using System.Text.Json;
using FtdModularLabs.Domain.Model;

namespace FtdModularLabs.Domain.Serialization;

/// <summary>Maps between persisted DTOs and the runtime domain entities.</summary>
public static class DomainMapper
{
    // ---- values ----

    private static Dictionary<string, JsonElement> ValuesToDto(IReadOnlyDictionary<string, object?> values) =>
        values.ToDictionary(kv => kv.Key, kv => JsonSerializer.SerializeToElement(kv.Value, DomainJson.Options));

    private static Dictionary<string, object?> ValuesFromDto(Dictionary<string, JsonElement> values) =>
        values.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

    // ---- VehicleDesign ----

    public static VehicleDesignDto ToDto(VehicleDesign d) => new(
        d.Id, d.Name, d.VehicleClass,
        d.Modules.Select(ToDto).ToList(),
        d.CreatedUtc, d.ModifiedUtc,
        d.Description, d.ManualCost);

    public static VehicleDesign FromDto(VehicleDesignDto dto) => new(
        dto.Id, dto.Name, dto.VehicleClass,
        dto.Modules.Select(FromDto),
        dto.CreatedUtc, dto.ModifiedUtc,
        dto.Description, dto.ManualCost);

    private static DesignModuleDto ToDto(DesignModule m) =>
        new(m.Id, m.Name, m.SubsystemTypeId, ValuesToDto(m.Values), ToDto(m.Contribution));

    private static DesignModule FromDto(DesignModuleDto dto) =>
        new(dto.Id, dto.Name, dto.SubsystemTypeId, ValuesFromDto(dto.Values), FromDto(dto.Contribution));

    // ---- ModuleContribution ----

    private static ModuleContributionDto? ToDto(ModuleContribution? c) => c is null
        ? null
        : new(c.Weight, c.CostFloor, c.Buoyancy, c.Lift, c.Volume, c.PowerOutput, c.PowerDraw);

    private static ModuleContribution? FromDto(ModuleContributionDto? dto) => dto is null
        ? null
        : new(dto.Weight, dto.CostFloor, dto.Buoyancy, dto.Lift, dto.Volume, dto.PowerOutput, dto.PowerDraw);

    // ---- Templates ----

    public static DesignTemplateDto ToDto(DesignTemplate t) => new(
        t.TemplateId, t.Name, t.VehicleClass,
        t.Modules.Select(ToDto).ToList());

    public static DesignTemplate FromDto(DesignTemplateDto dto, bool isBuiltIn) => new(
        dto.TemplateId, dto.Name, dto.VehicleClass,
        dto.Modules.Select(m => FromDto(m, isBuiltIn)).ToList(),
        isBuiltIn);

    public static ModuleTemplateDto ToDto(ModuleTemplate t) =>
        new(t.TemplateId, t.Name, t.SubsystemTypeId, ValuesToDto(t.Values));

    public static ModuleTemplate FromDto(ModuleTemplateDto dto, bool isBuiltIn) =>
        new(dto.TemplateId, dto.Name, dto.SubsystemTypeId, ValuesFromDto(dto.Values), isBuiltIn);
}
