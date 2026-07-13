using FtdModularLabs.Domain.Model;

namespace FtdModularLabs.Core.Tests;

public class VehicleStatsFloorTests
{
    [Fact]
    public void EmptyDesign_AllFloorsNull()
    {
        var floor = VehicleStatsFloor.FromModules(Array.Empty<DesignModule>());

        Assert.Null(floor.Weight);
        Assert.Null(floor.CostFloor);
        Assert.Null(floor.Buoyancy);
        Assert.Null(floor.Lift);
        Assert.Null(floor.Volume);
        Assert.Null(floor.PowerOutput);
        Assert.Null(floor.PowerDraw);
    }

    [Fact]
    public void SumsAcrossModules()
    {
        var a = new DesignModule(Guid.NewGuid(), "A", "weapon.aps",
            contribution: new ModuleContribution(Weight: 100, CostFloor: 500));
        var b = new DesignModule(Guid.NewGuid(), "B", "weapon.aps",
            contribution: new ModuleContribution(Weight: 100, CostFloor: 250));

        var floor = VehicleStatsFloor.FromModules(new[] { a, b });

        Assert.Equal(200.0, floor.Weight);
        Assert.Equal(750.0, floor.CostFloor);
    }

    [Fact]
    public void NullContributionsAreSkipped_NotSummedAsZero()
    {
        // A reports Weight; B reports no Weight (null). The floor is 100, not 100 + 0.
        var a = new DesignModule(Guid.NewGuid(), "A", "weapon.aps",
            contribution: new ModuleContribution(Weight: 100));
        var b = new DesignModule(Guid.NewGuid(), "B", "weapon.aps",
            contribution: new ModuleContribution(CostFloor: 42));
        // C has no contribution at all — module never computed.
        var c = new DesignModule(Guid.NewGuid(), "C", "weapon.aps");

        var floor = VehicleStatsFloor.FromModules(new[] { a, b, c });

        Assert.Equal(100.0, floor.Weight);
        Assert.Equal(42.0, floor.CostFloor);
        Assert.Null(floor.Volume);
    }

    [Fact]
    public void FromSummary_ReadsReservedKeys_AndIgnoresRest()
    {
        var summary = new Dictionary<string, object>
        {
            ["Recommended shell"] = "180mm HE",
            ["cost"] = 4200.0,
            ["volume"] = 850.0,
            ["powerDraw"] = 60.0,
        };

        var contribution = ModuleContribution.FromSummary(summary);

        Assert.Null(contribution.Weight);
        Assert.Equal(4200.0, contribution.CostFloor);
        Assert.Equal(850.0, contribution.Volume);
        Assert.Equal(60.0, contribution.PowerDraw);
    }

    [Fact]
    public void FromSummary_TolerantOfFloatsAndInts()
    {
        var summary = new Dictionary<string, object>
        {
            ["cost"] = 4200f,
            ["volume"] = 850,
        };

        var contribution = ModuleContribution.FromSummary(summary);

        Assert.Equal(4200.0, contribution.CostFloor);
        Assert.Equal(850.0, contribution.Volume);
    }

    [Fact]
    public void EmptyContribution_IsEmpty()
    {
        Assert.True(ModuleContribution.Empty.IsEmpty);
        Assert.False(new ModuleContribution(Weight: 1).IsEmpty);
    }
}
