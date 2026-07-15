using FtdModularLabs.App.Presentation;
using FtdModularLabs.Core;
using FtdModularLabs.Modules.Armor.Model;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// Headless spot-check of the armor-builder view-model logic — the behavior behind the Add/▲/▼/✕
/// buttons — since the Uno GUI itself can't be rendered in a headless test run.
/// </summary>
public class ArmorBuilderTests
{
    private static ParameterField LayerStackField() =>
        new(new ParameterDescriptor(
            Key: "targetArmor", Label: "Target armor", Kind: ParameterKind.LayerStack,
            Options: ArmorLayerLibrary.Names));

    [Fact]
    public void AddLayer_appends_the_selected_option()
    {
        var field = LayerStackField();
        field.SelectedOptionToAdd = ArmorLayer.HeavyBeam.Name;
        field.AddLayerCommand.Execute(null);
        field.SelectedOptionToAdd = ArmorLayer.MetalBeam.Name;
        field.AddLayerCommand.Execute(null);

        Assert.Equal(2, field.Stack.Count);
        Assert.Equal(ArmorLayer.HeavyBeam.Name, field.Stack[0].Name);
        Assert.Equal(ArmorLayer.MetalBeam.Name, field.Stack[1].Name);
    }

    [Fact]
    public void Duplicates_are_removed_by_identity_not_by_value()
    {
        var field = LayerStackField();
        field.SelectedOptionToAdd = ArmorLayer.WoodBeam.Name;
        field.AddLayerCommand.Execute(null);
        field.AddLayerCommand.Execute(null); // second identical wood beam
        var second = field.Stack[1];

        field.RemoveLayerCommand.Execute(second);

        Assert.Single(field.Stack);
        Assert.Same(field.Stack[0], field.Stack.Single()); // the first entry survived
    }

    [Fact]
    public void MoveUp_and_MoveDown_reorder_the_stack()
    {
        var field = LayerStackField();
        foreach (var n in new[] { "A", "B", "C" })
        {
            field.SelectedOptionToAdd = ArmorLayer.Air.Name; // any valid option
            field.AddLayerCommand.Execute(null);
        }
        // Rename via fresh entries to make order observable: rebuild deterministically.
        field.Stack.Clear();
        var e1 = AddNamed(field, ArmorLayer.HeavyBeam.Name);
        var e2 = AddNamed(field, ArmorLayer.MetalBeam.Name);
        var e3 = AddNamed(field, ArmorLayer.WoodBeam.Name);

        field.MoveLayerDownCommand.Execute(e1);   // H,M,W -> M,H,W
        Assert.Equal(new[] { e2, e1, e3 }, field.Stack);

        field.MoveLayerUpCommand.Execute(e3);      // M,H,W -> M,W,H
        Assert.Equal(new[] { e2, e3, e1 }, field.Stack);
    }

    [Fact]
    public void EffectiveValue_round_trips_into_the_module_as_an_ordered_name_list()
    {
        var field = LayerStackField();
        AddNamed(field, ArmorLayer.MetalBeam.Name);
        AddNamed(field, ArmorLayer.Air.Name);
        AddNamed(field, ArmorLayer.MetalBeam.Name);

        // This is exactly what ModuleRunnerViewModel.ComputeAsync passes to the module.
        var values = new ParameterValues().Set(field.Key, field.EffectiveValue);
        var names = values.GetStringList("targetArmor");

        Assert.Equal(new[] { "Metal 4m Beam", "Air", "Metal 4m Beam" }, names);

        // And the module accepts it and builds the intended 3-layer scheme.
        var scheme = new Scheme(names.Select(ArmorLayerLibrary.Find).Where(l => l is not null).Select(l => l!));
        Assert.Equal(3, scheme.LayerList.Count);
        // Air gap behind the front metal => no structural bonus, front AC stays raw 40.
        Assert.Equal(40f, scheme.LayerList[0].AC, 0.001f);
    }

    private static LayerStackEntry AddNamed(ParameterField field, string name)
    {
        field.SelectedOptionToAdd = name;
        field.AddLayerCommand.Execute(null);
        return field.Stack[^1];
    }
}
