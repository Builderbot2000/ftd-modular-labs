using System.Text.Json;
using FtdModularLabs.Core;
using FtdModularLabs.Domain.Serialization;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Armor;

namespace FtdModularLabs.Core.Tests;

public class ParameterValueSnapshotTests
{
    private static readonly ModuleSchema ApsSchema = new ApsShellModule().InputSchema;
    private static readonly ModuleSchema ArmorSchema = new ArmorModule().InputSchema;

    // A picked target-armor module reference persists as the referenced module's Guid string.
    private static readonly string ArmorRefId = Guid.NewGuid().ToString();

    private static ParameterValues SampleApsValues()
    {
        var values = new ParameterValues();
        values.Set("targetArmor", ArmorRefId); // ModuleReference (Guid string)
        values.Set("damageType", "HE");
        values.Set("optimizeFor", "DPS per volume");
        values.Set("minGauge", 120.0);
        values.Set("maxGauge", 480.0);
        values.Set("maxLoaderLength", 4);
        values.Set("impactAngle", 30.0);
        values.Set("allowRail", true);
        return values;
    }

    private static ParameterValues SampleArmorValues()
    {
        var values = new ParameterValues();
        values.Set("targetArmor", new List<string> { "HA 4m Beam", "Metal 4m Beam", "Air" }); // LayerStack
        values.Set("attackingAps", ""); // unset ModuleReference
        values.Set("impactAngle", 45.0);
        return values;
    }

    [Fact]
    public void Capture_Then_Restore_InMemory_RoundTrips()
    {
        var captured = ParameterValueSnapshot.Capture(SampleApsValues(), ApsSchema);
        var restored = ParameterValueSnapshot.Restore(captured, ApsSchema);

        Assert.Equal(ArmorRefId, restored.GetString("targetArmor"));
        Assert.Equal("HE", restored.GetEnumOption("damageType"));
        Assert.Equal(120.0, restored.GetDouble("minGauge"));
        Assert.Equal(4, restored.GetInt("maxLoaderLength"));
        Assert.True(restored.GetBool("allowRail"));
    }

    [Fact]
    public void Capture_Serialize_Deserialize_Restore_RoundTripsThroughJson()
    {
        var captured = ParameterValueSnapshot.Capture(SampleApsValues(), ApsSchema);

        // Simulate the on-disk path: values become JsonElements after deserialization.
        var json = JsonSerializer.Serialize(captured, DomainJson.Options);
        var reloaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, DomainJson.Options)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

        var restored = ParameterValueSnapshot.Restore(reloaded, ApsSchema);

        Assert.Equal(ArmorRefId, restored.GetString("targetArmor"));
        Assert.Equal("DPS per volume", restored.GetEnumOption("optimizeFor"));
        Assert.Equal(480.0, restored.GetDouble("maxGauge"));
        Assert.Equal(30.0, restored.GetDouble("impactAngle"));
        Assert.Equal(4, restored.GetInt("maxLoaderLength"));
        Assert.True(restored.GetBool("allowRail"));

        // And the restored values pass the module's own validation.
        Assert.Empty(restored.Validate(ApsSchema));
    }

    [Fact]
    public void LayerStack_RoundTrips_InMemory_AndThroughJson()
    {
        var captured = ParameterValueSnapshot.Capture(SampleArmorValues(), ArmorSchema);

        var inMemory = ParameterValueSnapshot.Restore(captured, ArmorSchema);
        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Air" }, inMemory.GetStringList("targetArmor"));

        // Simulate the on-disk path: the LayerStack becomes a JsonElement array.
        var json = JsonSerializer.Serialize(captured, DomainJson.Options);
        var reloaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, DomainJson.Options)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        var throughJson = ParameterValueSnapshot.Restore(reloaded, ArmorSchema);

        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Air" }, throughJson.GetStringList("targetArmor"));
        Assert.Equal(45.0, throughJson.GetDouble("impactAngle"));
        Assert.Empty(throughJson.WithDefaults(ArmorSchema).Validate(ArmorSchema));
    }

    [Fact]
    public void Capture_WithNullSchema_ReturnsEmpty()
    {
        var captured = ParameterValueSnapshot.Capture(SampleApsValues(), schema: null);
        Assert.Empty(captured);
    }

    [Fact]
    public void Restore_IgnoresUnknownKeys()
    {
        var raw = new Dictionary<string, object?> { ["nonexistent"] = "x", ["minGauge"] = 200.0 };
        var restored = ParameterValueSnapshot.Restore(raw, ApsSchema);
        Assert.Equal(200.0, restored.GetDouble("minGauge"));
        Assert.False(restored.Contains("nonexistent"));
    }
}
