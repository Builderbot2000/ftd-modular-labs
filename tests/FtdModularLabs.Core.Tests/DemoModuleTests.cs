using FtdModularLabs.Core;
using FtdModularLabs.Modules.Demo;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Core.Tests;

public class DemoModuleTests
{
    [Fact]
    public void KineticEnergy_matches_half_m_v_squared()
    {
        // ½ · 10 · 1000² = 5,000,000
        Assert.Equal(5_000_000d, KineticEnergyModule.KineticEnergy(10, 1000));
    }

    [Fact]
    public async Task ComputeAsync_returns_expected_summary_and_table()
    {
        var module = new KineticEnergyModule();
        var inputs = new ParameterValues()
            .Set("mass", 10.0).Set("velocity", 1000.0).Set("material", "Tungsten");

        var result = await module.ComputeAsync(inputs, CancellationToken.None);

        Assert.Equal("5,000,000 J", result.Summary["Kinetic energy"]);
        Assert.Equal("Tungsten", result.Summary["Material"]);

        var table = Assert.Single(result.Tables);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(2, table.Columns.Count);
        // Third row is 3× velocity => 3000 m/s.
        Assert.Equal("3,000", table.Rows[2][0]);
    }

    [Fact]
    public async Task ComputeAsync_uses_defaults_when_values_omitted()
    {
        var module = new KineticEnergyModule();
        var result = await module.ComputeAsync(new ParameterValues(), CancellationToken.None);

        // Defaults: mass 10, velocity 1000 => 5,000,000 J, material Steel.
        Assert.Equal("5,000,000 J", result.Summary["Kinetic energy"]);
        Assert.Equal("Steel", result.Summary["Material"]);
    }

    [Fact]
    public void AddDemoModule_registers_discoverable_module()
    {
        var provider = new ServiceCollection()
            .AddDemoModule()
            .BuildServiceProvider();

        var modules = provider.GetServices<ICalculationModule>().ToList();
        var demo = Assert.Single(modules);
        Assert.Equal(KineticEnergyModule.ModuleId, demo.Id);
    }
}
