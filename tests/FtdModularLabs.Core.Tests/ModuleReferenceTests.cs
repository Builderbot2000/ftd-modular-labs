using System.Text.Json;
using FtdModularLabs.Core;
using FtdModularLabs.Domain.Serialization;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Armor;
using FtdModularLabs.Modules.Armor.Model;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// The <see cref="ParameterKind.ModuleReference"/> contract: persisted as a Guid string, resolved by
/// the module editor to the referenced module's values before compute, and read back by a calculator
/// via <see cref="ParameterValues.GetReferencedValues"/>.
/// </summary>
public class ModuleReferenceTests
{
    private static readonly ModuleSchema RefSchema = new(new[]
    {
        new ParameterDescriptor(
            Key: "ref", Label: "Ref", Kind: ParameterKind.ModuleReference,
            Default: "", ReferenceSubsystemTypeId: "defence.armor"),
    });

    private static readonly ModuleSchema ArmorSchema = new ArmorModule().InputSchema;

    // Serialize then deserialize to JsonElements — how a module's Values look freshly loaded from disk.
    private static Dictionary<string, object?> OnDiskValues(Dictionary<string, object?> values)
    {
        var json = JsonSerializer.Serialize(values, DomainJson.Options);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, DomainJson.Options)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
    }

    [Fact]
    public void GetReferencedValues_unwraps_a_dictionary()
    {
        var refValues = new Dictionary<string, object?> { ["targetArmor"] = new List<string> { "Metal 4m Beam" } };
        var values = new ParameterValues().Set("ref", refValues);

        var referenced = values.GetReferencedValues("ref");
        Assert.Equal(new[] { "Metal 4m Beam" }, referenced.GetStringList("targetArmor"));
    }

    [Fact]
    public void GetReferencedValues_passes_through_a_ParameterValues()
    {
        var inner = new ParameterValues().Set("targetArmor", new List<string> { "HA 4m Beam" });
        var values = new ParameterValues().Set("ref", inner);

        var referenced = values.GetReferencedValues("ref");
        Assert.Equal(new[] { "HA 4m Beam" }, referenced.GetStringList("targetArmor"));
    }

    [Fact]
    public void GetReferencedValues_on_an_unresolved_guid_string_throws()
    {
        // A picked-but-unresolved reference is still just its Guid string; the calculator must not
        // be handed one — the editor resolves it first. Surfacing this as an error is intentional.
        var values = new ParameterValues().Set("ref", Guid.NewGuid().ToString());
        Assert.Throws<InvalidOperationException>(() => values.GetReferencedValues("ref"));
    }

    [Fact]
    public void GetReferencedValues_on_a_missing_key_throws()
    {
        var values = new ParameterValues();
        Assert.Throws<KeyNotFoundException>(() => values.GetReferencedValues("ref"));
    }

    [Theory]
    [InlineData("")]                                  // unset reference
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")] // a picked Guid
    public void Validation_accepts_string_references(string value)
    {
        var errors = new ParameterValues().Set("ref", value).Validate(RefSchema);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validation_accepts_a_resolved_reference()
    {
        var resolvedDict = new ParameterValues().Set("ref", new Dictionary<string, object?>());
        Assert.Empty(resolvedDict.Validate(RefSchema));

        var resolvedPv = new ParameterValues().Set("ref", new ParameterValues());
        Assert.Empty(resolvedPv.Validate(RefSchema));
    }

    [Fact]
    public void Validation_rejects_a_non_reference_value()
    {
        var errors = new ParameterValues().Set("ref", 42).Validate(RefSchema);
        Assert.Contains(errors, e => e.Contains("must reference a module"));
    }

    [Fact]
    public void Reference_persists_as_a_string_through_the_snapshot()
    {
        var id = Guid.NewGuid().ToString();
        var captured = ParameterValueSnapshot.Capture(new ParameterValues().Set("ref", id), RefSchema);

        var json = JsonSerializer.Serialize(captured, DomainJson.Options);
        var reloaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, DomainJson.Options)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        var restored = ParameterValueSnapshot.Restore(reloaded, RefSchema);

        Assert.Equal(id, restored.GetString("ref"));
    }

    [Fact]
    public async Task Aps_reads_layers_from_a_reference_normalized_through_its_schema()
    {
        // The referenced armor module fresh off disk (raw JsonElement values), resolved the way the
        // module editor does: normalized through the armor schema into a ParameterValues.
        var onDiskArmor = OnDiskValues(new Dictionary<string, object?>
        {
            ["targetArmor"] = new List<string> { ArmorLayer.WoodBeam.Name },
        });
        var resolvedRef = ParameterValueSnapshot.Restore(onDiskArmor, ArmorSchema);

        var result = await new ApsShellModule().ComputeAsync(
            ApsInputs().Set("targetArmor", resolvedRef), CancellationToken.None);

        Assert.True(result.Summary.ContainsKey("Recommended shell"));
        Assert.Equal("8.0", result.Summary["Target front AC"]); // the single Wood 4m Beam (AC 8)
    }

    [Fact]
    public async Task Aps_against_an_unnormalized_reference_sees_an_empty_stack()
    {
        // Handing the calculator the referenced module's *raw* JsonElement dict (not normalized
        // through its schema) reads no layers: the search runs against an empty scheme (front AC "—").
        // This is exactly why ModuleEditorViewModel normalizes the reference through its schema first.
        var onDiskArmor = OnDiskValues(new Dictionary<string, object?>
        {
            ["targetArmor"] = new List<string> { ArmorLayer.WoodBeam.Name },
        });

        var result = await new ApsShellModule().ComputeAsync(
            ApsInputs().Set("targetArmor", onDiskArmor), CancellationToken.None);

        Assert.Equal("—", result.Summary["Target front AC"]);
    }

    private static ParameterValues ApsInputs() => new ParameterValues()
        .Set("optimizeFor", "DPS per cost")
        .Set("minGauge", 100.0).Set("maxGauge", 500.0)
        .Set("maxLoaderLength", 4).Set("impactAngle", 45.0).Set("allowRail", false);
}
