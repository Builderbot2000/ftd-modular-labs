using System.Text.Json;
using FtdOptima.Core;
using FtdOptima.Domain.Serialization;
using FtdOptima.Modules.Aps;

namespace FtdOptima.Core.Tests;

public class ParameterValueSnapshotTests
{
    private static readonly ModuleSchema ApsSchema = new ApsShellModule().InputSchema;

    private static ParameterValues SampleApsValues()
    {
        var values = new ParameterValues();
        values.Set("targetArmor", new List<string> { "HA 4m Beam", "Metal 4m Beam", "Air" });
        values.Set("damageType", "HE");
        values.Set("optimizeFor", "DPS per volume");
        values.Set("minGauge", 120.0);
        values.Set("maxGauge", 480.0);
        values.Set("maxLoaderLength", 4);
        values.Set("impactAngle", 30.0);
        values.Set("allowRail", true);
        return values;
    }

    [Fact]
    public void Capture_Then_Restore_InMemory_RoundTrips()
    {
        var captured = ParameterValueSnapshot.Capture(SampleApsValues(), ApsSchema);
        var restored = ParameterValueSnapshot.Restore(captured, ApsSchema);

        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Air" }, restored.GetStringList("targetArmor"));
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

        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Air" }, restored.GetStringList("targetArmor"));
        Assert.Equal("DPS per volume", restored.GetEnumOption("optimizeFor"));
        Assert.Equal(480.0, restored.GetDouble("maxGauge"));
        Assert.Equal(30.0, restored.GetDouble("impactAngle"));
        Assert.Equal(4, restored.GetInt("maxLoaderLength"));
        Assert.True(restored.GetBool("allowRail"));

        // And the restored values pass the module's own validation.
        Assert.Empty(restored.Validate(ApsSchema));
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
