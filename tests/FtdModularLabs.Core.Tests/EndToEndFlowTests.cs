using FtdModularLabs.Domain.Catalog;
using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Serialization;
using FtdModularLabs.Domain.Storage;
using FtdModularLabs.Modules.Aps;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// Exercises the full app flow at the domain/persistence layer (the logic the UI binds to):
/// create a design from the built-in Ship template, add a calculator-less module, run the APS
/// calculator on a module, persist, reload, and confirm everything round-trips.
/// </summary>
public class EndToEndFlowTests
{
    [Fact]
    public async Task CreateFromTemplate_AddModule_Compute_Persist_Reload()
    {
        using var paths = new TempPaths();
        var registry = new SubsystemTypeRegistry(new[] { new ApsShellModule() });
        var designs = new JsonFileVehicleDesignRepository(paths);
        var templates = new JsonFileTemplateRepository(paths);

        // 1. Create "Valiant-class Battleship" from the built-in Ship template.
        var ship = (await templates.GetDesignTemplatesAsync()).First(t => t.TemplateId == "ship");
        var design = DesignFactory.CreateFromTemplate(ship, "Valiant-class Battleship");
        Assert.Equal(3, design.Modules.Count); // Main Battery (APS), LAMS, Steam Engine

        // 2. Add a calculator-less CRAM module — nameable/persistable, no schema.
        var cram = DesignFactory.CreateBlankModule(SubsystemCatalog.Find("weapon.cram")!, "Bow CIWS");
        design.Modules.Add(cram);
        Assert.Null(registry.GetSchema("weapon.cram"));

        // 3. Run the APS calculator on the Main Battery module using its persisted values.
        var mainBattery = design.Modules.First(m => m.SubsystemTypeId == "weapon.aps");
        var calc = registry.GetCalculator("weapon.aps")!;
        var schema = registry.GetSchema("weapon.aps")!;
        var values = ParameterValueSnapshot.Restore(mainBattery.Values, schema).WithDefaults(schema);
        Assert.Empty(values.Validate(schema));
        var result = await calc.ComputeAsync(values, CancellationToken.None);
        Assert.NotEmpty(result.Summary);

        // 4. Persist, then reload from disk.
        await designs.SaveAsync(design);
        var reloaded = await designs.GetAsync(design.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Valiant-class Battleship", reloaded!.Name);
        Assert.Equal(4, reloaded.Modules.Count);
        Assert.Contains(reloaded.Modules, m => m.Name == "Bow CIWS" && m.SubsystemTypeId == "weapon.cram");

        // APS values survived the round-trip and still validate.
        var reloadedAps = reloaded.Modules.First(m => m.SubsystemTypeId == "weapon.aps");
        var reloadedValues = ParameterValueSnapshot.Restore(reloadedAps.Values, schema);
        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Metal 4m Beam" }, reloadedValues.GetStringList("targetArmor"));
        Assert.Equal("Kinetic", reloadedValues.GetEnumOption("damageType"));
        Assert.Empty(reloadedValues.WithDefaults(schema).Validate(schema));
    }
}
