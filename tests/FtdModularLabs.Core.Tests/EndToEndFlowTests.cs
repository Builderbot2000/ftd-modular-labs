using FtdModularLabs.Core;
using FtdModularLabs.Domain.Catalog;
using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Serialization;
using FtdModularLabs.Domain.Storage;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Armor;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// Exercises the full app flow at the domain/persistence layer (the logic the UI binds to):
/// create a design from the built-in Ship template, add a calculator-less module, resolve the APS
/// module's target-armor <see cref="ParameterKind.ModuleReference"/> to the design's Armor module,
/// run the shell search, persist, reload, and confirm everything round-trips.
/// </summary>
public class EndToEndFlowTests
{
    [Fact]
    public async Task CreateFromTemplate_AddModule_Compute_Persist_Reload()
    {
        using var paths = new TempPaths();
        var registry = new SubsystemTypeRegistry(new ICalculationModule[] { new ApsShellModule(), new ArmorModule() });
        var designs = new JsonFileVehicleDesignRepository(paths);
        var templates = new JsonFileTemplateRepository(paths);

        // 1. Create "Valiant-class Battleship" from the built-in Ship template.
        var ship = (await templates.GetDesignTemplatesAsync()).First(t => t.TemplateId == "ship");
        var design = DesignFactory.CreateFromTemplate(ship, "Valiant-class Battleship");
        Assert.Equal(4, design.Modules.Count); // Belt Armor, Main Battery (APS), LAMS, Steam Engine

        // 2. Add a calculator-less CRAM module — nameable/persistable, no schema.
        var cram = DesignFactory.CreateBlankModule(SubsystemCatalog.Find("weapon.cram")!, "Bow CIWS");
        design.Modules.Add(cram);
        Assert.Null(registry.GetSchema("weapon.cram"));

        var apsSchema = registry.GetSchema("weapon.aps")!;
        var armorSchema = registry.GetSchema("defence.armor")!;
        var calc = registry.GetCalculator("weapon.aps")!;

        // 3. Run the APS calculator on the Main Battery, resolving its targetArmor ModuleReference to
        //    the Belt Armor module — exactly what the module editor does before compute (the referenced
        //    module's raw values are normalized through its own schema, then handed to the calculator).
        var mainBattery = design.Modules.First(m => m.SubsystemTypeId == "weapon.aps");
        var beltArmor = design.Modules.First(m => m.SubsystemTypeId == "defence.armor");
        var values = ParameterValueSnapshot.Restore(mainBattery.Values, apsSchema).WithDefaults(apsSchema);
        values.Set("targetArmor", ParameterValueSnapshot.Restore(beltArmor.Values, armorSchema));
        Assert.Empty(values.Validate(apsSchema));

        var result = await calc.ComputeAsync(values, CancellationToken.None);
        Assert.NotEmpty(result.Summary);
        // The reference resolved: the search ran against a real armor stack, not the "pick a target" stub.
        Assert.DoesNotContain("no target armor selected", result.Notes);

        // 4. Persist, then reload from disk.
        await designs.SaveAsync(design);
        var reloaded = await designs.GetAsync(design.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Valiant-class Battleship", reloaded!.Name);
        Assert.Equal(5, reloaded.Modules.Count);
        Assert.Contains(reloaded.Modules, m => m.Name == "Bow CIWS" && m.SubsystemTypeId == "weapon.cram");

        // The Belt Armor layer stack survived the round-trip and still validates.
        var reloadedArmor = reloaded.Modules.First(m => m.SubsystemTypeId == "defence.armor");
        var reloadedArmorValues = ParameterValueSnapshot.Restore(reloadedArmor.Values, armorSchema);
        Assert.Equal(new[] { "HA 4m Beam", "Metal 4m Beam", "Metal 4m Beam" }, reloadedArmorValues.GetStringList("targetArmor"));
        Assert.Empty(reloadedArmorValues.WithDefaults(armorSchema).Validate(armorSchema));

        // And the APS module still resolves its reference against the reloaded armor and computes.
        var reloadedBattery = reloaded.Modules.First(m => m.SubsystemTypeId == "weapon.aps");
        var reloadedValues = ParameterValueSnapshot.Restore(reloadedBattery.Values, apsSchema).WithDefaults(apsSchema);
        reloadedValues.Set("targetArmor", ParameterValueSnapshot.Restore(reloadedArmor.Values, armorSchema));
        var reloadedResult = await calc.ComputeAsync(reloadedValues, CancellationToken.None);
        Assert.NotEmpty(reloadedResult.Summary);
        Assert.DoesNotContain("no target armor selected", reloadedResult.Notes);
    }
}
