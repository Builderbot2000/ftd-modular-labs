using FtdModularLabs.Core;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Armor;
using FtdModularLabs.Modules.Armor.Model;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// Behaviour of the extracted <see cref="ArmorModule"/>: it characterizes a layer stack and — when
/// an attacking APS reference is resolved and an <see cref="IBreachRater"/> is available — reports a
/// breach rating. The APS-side math is reached only through the <see cref="IBreachRater"/> seam.
/// </summary>
public class ArmorModuleTests
{
    private static ParameterValues Stack(params string[] layers) =>
        new ParameterValues().Set("targetArmor", layers.ToList());

    [Fact]
    public async Task Characterizes_a_layer_stack()
    {
        var module = new ArmorModule();
        var result = await module.ComputeAsync(
            Stack(ArmorLayer.HeavyBeam.Name, ArmorLayer.MetalBeam.Name, ArmorLayer.MetalBeam.Name),
            CancellationToken.None);

        Assert.Equal("3", result.Summary["Layer count"]);
        Assert.True(result.Summary.ContainsKey("Total HP"));
        Assert.True(result.Summary.ContainsKey("Avg AC (HP-weighted)"));
        Assert.True(result.Summary.ContainsKey("Front layer AC"));

        // One table (the layer list), one row per layer; no breach section without an attacking APS.
        var table = Assert.Single(result.Tables);
        Assert.Equal(3, table.Rows.Count);
        Assert.False(result.Summary.ContainsKey("Breach verdict"));
        Assert.False(result.Summary.ContainsKey("Breach rating"));
    }

    [Fact]
    public async Task Empty_stack_asks_for_layers()
    {
        var module = new ArmorModule();
        var result = await module.ComputeAsync(
            new ParameterValues().Set("targetArmor", new List<string>()),
            CancellationToken.None);

        Assert.Equal("Add at least one armor layer.", result.Summary["Result"]);
        Assert.Empty(result.Tables);
    }

    [Fact]
    public async Task Rates_breach_when_an_attacking_APS_is_resolved()
    {
        // The module editor resolves the attackingAps ModuleReference to the APS module's values;
        // here we hand in a bag the APS defaults fill out, plus a registered breach rater.
        var module = new ArmorModule(new ApsBreachRater());
        var inputs = Stack(ArmorLayer.WoodBeam.Name, ArmorLayer.WoodBeam.Name)
            .Set("attackingAps", new ParameterValues());

        var result = await module.ComputeAsync(inputs, CancellationToken.None);

        Assert.True(result.Summary.ContainsKey("Breach verdict"));
    }

    [Fact]
    public async Task Skips_rating_when_no_breach_rater_is_registered()
    {
        var module = new ArmorModule(rater: null);
        var inputs = Stack(ArmorLayer.MetalBeam.Name)
            .Set("attackingAps", new ParameterValues());

        var result = await module.ComputeAsync(inputs, CancellationToken.None);

        Assert.Equal("APS rating unavailable — no IBreachRater registered.", result.Summary["Breach rating"]);
        Assert.False(result.Summary.ContainsKey("Breach verdict"));
    }

    [Fact]
    public void AddArmorModule_registers_discoverable_module()
    {
        var provider = new ServiceCollection().AddArmorModule().BuildServiceProvider();
        var module = Assert.Single(provider.GetServices<ICalculationModule>());
        Assert.Equal(ArmorModule.ModuleId, module.Id);
    }

    [Fact]
    public async Task With_Aps_registered_the_armor_module_gets_a_wired_breach_rater()
    {
        // The composition root registers both modules; the Armor module pulls the APS-provided
        // IBreachRater from DI, so its compute produces a breach rating without a direct dependency.
        var provider = new ServiceCollection()
            .AddArmorModule()
            .AddApsModule()
            .BuildServiceProvider();

        var armor = provider.GetServices<ICalculationModule>().OfType<ArmorModule>().Single();
        var inputs = Stack(ArmorLayer.WoodBeam.Name, ArmorLayer.WoodBeam.Name)
            .Set("attackingAps", new ParameterValues());

        var result = await armor.ComputeAsync(inputs, CancellationToken.None);

        Assert.True(result.Summary.ContainsKey("Breach verdict"));
        Assert.False(result.Summary.ContainsKey("Breach rating")); // i.e. not the "unavailable" branch
    }
}
