using FtdOptima.Domain.Catalog;
using FtdOptima.Domain.Model;
using FtdOptima.Domain.Storage;
using FtdOptima.Modules.Aps;

namespace FtdOptima.Core.Tests;

/// <summary>A throwaway app-data root under the system temp dir, deleted on dispose.</summary>
internal sealed class TempPaths : IAppDataPaths, IDisposable
{
    public TempPaths()
    {
        Root = Path.Combine(Path.GetTempPath(), "ftdoptima-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public void Dispose()
    {
        try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); }
        catch { /* best effort */ }
    }
}

public class VehicleDesignRepositoryTests
{
    [Fact]
    public async Task Save_Get_GetAll_Delete_RoundTrip()
    {
        using var paths = new TempPaths();
        var repo = new JsonFileVehicleDesignRepository(paths);

        var design = DesignFactory.CreateBlank("Valiant-class Battleship", "Ship");
        design.Modules.Add(new DesignModule(Guid.NewGuid(), "Main Battery", "weapon.aps",
            new Dictionary<string, object?> { ["minGauge"] = 200.0, ["targetArmor"] = new List<string> { "Heavy Beam" } }));
        await repo.SaveAsync(design);

        var all = await repo.GetAllAsync();
        Assert.Single(all);

        var loaded = await repo.GetAsync(design.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Valiant-class Battleship", loaded!.Name);
        Assert.Equal("Ship", loaded.VehicleClass);
        Assert.Single(loaded.Modules);
        Assert.Equal("Main Battery", loaded.Modules[0].Name);
        Assert.Equal("weapon.aps", loaded.Modules[0].SubsystemTypeId);

        await repo.DeleteAsync(design.Id);
        Assert.Empty(await repo.GetAllAsync());
        Assert.Null(await repo.GetAsync(design.Id));
    }

    [Fact]
    public async Task GetAll_OnMissingDir_ReturnsEmpty()
    {
        using var paths = new TempPaths();
        var repo = new JsonFileVehicleDesignRepository(paths);
        Assert.Empty(await repo.GetAllAsync());
    }
}

public class TemplateRepositoryTests
{
    [Fact]
    public async Task BuiltIns_AreAvailable_AndReadOnly()
    {
        using var paths = new TempPaths();
        var repo = new JsonFileTemplateRepository(paths);

        var designs = await repo.GetDesignTemplatesAsync();
        Assert.Contains(designs, t => t.TemplateId == "ship" && t.IsBuiltIn);
        var ship = designs.First(t => t.TemplateId == "ship");
        Assert.Contains(ship.Modules, m => m.SubsystemTypeId == "weapon.aps");

        var apsModules = await repo.GetModuleTemplatesAsync("weapon.aps");
        Assert.NotEmpty(apsModules);
        Assert.All(apsModules, m => Assert.Equal("weapon.aps", m.SubsystemTypeId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteDesignTemplateAsync("ship"));
    }

    [Fact]
    public async Task SaveAndDelete_UserTemplate()
    {
        using var paths = new TempPaths();
        var repo = new JsonFileTemplateRepository(paths);

        var design = DesignFactory.CreateBlank("My Cruiser", "Ship");
        var template = DesignFactory.SnapshotAsTemplate(design, "My Cruiser Template");
        Assert.False(template.IsBuiltIn);

        await repo.SaveDesignTemplateAsync(template);
        var all = await repo.GetDesignTemplatesAsync();
        Assert.Contains(all, t => t.TemplateId == template.TemplateId && !t.IsBuiltIn);

        await repo.DeleteDesignTemplateAsync(template.TemplateId);
        all = await repo.GetDesignTemplatesAsync();
        Assert.DoesNotContain(all, t => t.TemplateId == template.TemplateId);
    }
}

public class BuiltInTemplateValidityTests
{
    private static readonly SubsystemTypeRegistry Registry = new(new[] { new ApsShellModule() });

    [Fact]
    public void EveryBuiltInModuleTemplate_HasKnownTypeAndValidValues()
    {
        Assert.NotEmpty(BuiltInTemplates.Modules);
        foreach (var t in BuiltInTemplates.Modules)
            AssertTemplateValid(t.SubsystemTypeId, t.Values, t.Name);
    }

    [Fact]
    public void EveryBuiltInDesignTemplate_HasKnownTypesAndValidValues()
    {
        Assert.NotEmpty(BuiltInTemplates.Designs);
        foreach (var d in BuiltInTemplates.Designs)
            foreach (var m in d.Modules)
                AssertTemplateValid(m.SubsystemTypeId, m.Values, $"{d.Name}/{m.Name}");
    }

    private static void AssertTemplateValid(string subsystemTypeId, IReadOnlyDictionary<string, object?> values, string label)
    {
        Assert.NotNull(SubsystemCatalog.Find(subsystemTypeId));
        var schema = Registry.GetSchema(subsystemTypeId);
        if (schema is null)
            return; // calculator-less type: values are free-form (expected empty)
        var restored = FtdOptima.Domain.Serialization.ParameterValueSnapshot.Restore(values, schema);
        var errors = restored.WithDefaults(schema).Validate(schema);
        Assert.True(errors.Count == 0, $"Template '{label}' has invalid values: {string.Join("; ", errors)}");
    }
}

public class DesignFactoryTests
{
    [Fact]
    public void CreateFromTemplate_DeepCopies_Independent()
    {
        var moduleValues = new Dictionary<string, object?> { ["targetArmor"] = new List<string> { "Heavy Beam" } };
        var moduleTemplate = new ModuleTemplate("mt", "Gun", "weapon.aps", moduleValues, IsBuiltIn: true);
        var template = new DesignTemplate("dt", "Ship", "Ship", new[] { moduleTemplate }, IsBuiltIn: true);

        var a = DesignFactory.CreateFromTemplate(template, "A");
        var b = DesignFactory.CreateFromTemplate(template, "B");

        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(a.Modules[0].Id, b.Modules[0].Id);

        // Mutating one design's module values must not touch the template or the other design.
        ((List<string>)a.Modules[0].Values["targetArmor"]!).Add("Metal Beam");
        Assert.Single((List<string>)b.Modules[0].Values["targetArmor"]!);
        Assert.Single((List<string>)moduleValues["targetArmor"]!);
    }

    [Fact]
    public void SnapshotAsTemplate_ProducesUserTemplate()
    {
        var design = DesignFactory.CreateBlank("X", "Ship");
        design.Modules.Add(DesignFactory.CreateBlankModule(SubsystemCatalog.Find("weapon.aps")!, "Gun"));
        var t = DesignFactory.SnapshotAsTemplate(design, "T");
        Assert.False(t.IsBuiltIn);
        Assert.Single(t.Modules);
    }
}

public class SubsystemTypeRegistryTests
{
    [Fact]
    public void ResolvesApsCalculator_AndNullForCalculatorlessTypes()
    {
        var registry = new SubsystemTypeRegistry(new[] { new ApsShellModule() });

        Assert.NotNull(registry.GetCalculator("weapon.aps"));
        Assert.NotNull(registry.GetSchema("weapon.aps"));

        Assert.Null(registry.GetCalculator("power.steam-engine"));
        Assert.Null(registry.GetSchema("power.steam-engine"));

        Assert.Equal("APS Turret", registry.FindType("weapon.aps")!.Name);
        Assert.False(registry.FindType("power.steam-engine")!.HasCalculator);
    }
}
