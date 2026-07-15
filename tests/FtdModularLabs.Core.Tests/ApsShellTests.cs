using FtdModularLabs.Core;
using FtdModularLabs.Modules.Aps;
using FtdModularLabs.Modules.Aps.Model;
using FtdModularLabs.Modules.Armor;
using FtdModularLabs.Modules.Armor.Model;
using Microsoft.Extensions.DependencyInjection;

namespace FtdModularLabs.Core.Tests;

/// <summary>
/// Correctness gate for the APS port: reproduces known columns of AoKishuba's
/// "APS Shell Configurations" spreadsheet (Gunpowder Kinetic) and checks the penetration model.
/// </summary>
public class ApsShellTests
{
    // Body-module count array sized to the Middle-module registry, with the given solid/sabot mix.
    private static float[] Body(int solid, int sabot = 0)
    {
        var body = new float[ApsModule.BodyModules.Length];
        body[ApsModule.SolidBodyIndex] = solid;
        body[ApsModule.SabotBodyIndex] = sabot;
        return body;
    }

    // Empty scheme => shell is treated as penetrating and applied-damage uses angle only (front AC 0).
    private static Shell EvaluateBare(Shell shell)
    {
        shell.EvaluateKinetic(new Scheme(), new TestConditions());
        return shell;
    }

    [Fact]
    public void GunpowderKinetic_100mm_1mLoader_matches_spreadsheet()
    {
        // Spreadsheet "Gunpowder Kinetic / No Base / 1 m loader": 100 mm, 7 GP casings,
        // 2 solid bodies, Heavy head. Expected V 1123, AP 19.7, RawKD 1525, reload 13.45.
        var shell = new Shell(100f, ApsModule.HeavyHead, null, Body(solid: 2), gpCasingCount: 7, rgCasingCount: 0, railDraw: 0f);
        EvaluateBare(shell);

        Assert.Equal(966f, shell.TotalRecoil, 1f);   // sheet: 966 (965.8 unrounded)
        Assert.Equal(1123f, shell.Velocity, 1f);      // sheet: 1123
        Assert.Equal(19.7f, shell.ArmorPierce, 0.1f); // sheet: 19.7
        Assert.Equal(1525f, shell.RawKD, 2f);         // sheet: 1525
        Assert.Equal(1078f, shell.KineticDamage, 2f); // sheet: 1078 (RawKD × cos45°)
        Assert.Equal(13.45f, shell.ClusterReloadTime, 0.02f);

        // Volume/cost accounting reproduces the DPS-per-* columns (20.633 / 0.14269).
        Assert.Equal(80.1f, shell.KineticDps, 0.3f);
        Assert.Equal(20.63f, shell.KineticDpsPerVolume, 0.1f);
        Assert.Equal(0.1427f, shell.KineticDpsPerCost, 0.001f);
    }

    [Fact]
    public void GunpowderKinetic_200mm_4mLoader_matches_spreadsheet()
    {
        // Spreadsheet "Gunpowder Kinetic / No Base / 4 m loader": 200 mm, 14 GP casings,
        // 5 solid bodies, Heavy head. Expected V 1123, AP 19.7, RawKD 9569, reload 58.41.
        var shell = new Shell(200f, ApsModule.HeavyHead, null, Body(solid: 5), gpCasingCount: 14, rgCasingCount: 0, railDraw: 0f);
        EvaluateBare(shell);

        Assert.Equal(6726f, shell.TotalRecoil, 2f);
        Assert.Equal(1123f, shell.Velocity, 1f);
        Assert.Equal(19.7f, shell.ArmorPierce, 0.1f);
        Assert.Equal(9569f, shell.RawKD, 5f);
        Assert.Equal(58.41f, shell.ClusterReloadTime, 0.05f);
    }

    [Fact]
    public void SabotHead_boosts_AP_and_cuts_KD_per_module_table()
    {
        // AP head vs Sabot head at identical geometry: sabot AP mod 2.5 vs AP head 1.65; KD 0.85 vs 1.0.
        var ap = EvaluateBare(new Shell(150f, ApsModule.APHead, null, Body(2), 6, 0, 0f));
        var sabot = EvaluateBare(new Shell(150f, ApsModule.SabotHead, null, Body(2), 6, 0, 0f));

        Assert.True(sabot.ArmorPierce > ap.ArmorPierce);
        Assert.True(sabot.RawKD < ap.RawKD);
    }

    [Fact]
    public void Scheme_applies_structural_bonus_one_layer_deep()
    {
        // Wood beam (AC 8) backed by a wood beam: front layer gets +0.2·8 = 9.6; back stays 8.
        var scheme = new Scheme([ArmorLayer.WoodBeam, ArmorLayer.WoodBeam]);
        Assert.Equal(9.6f, scheme.LayerList[0].AC, 0.001f);
        Assert.Equal(8f, scheme.LayerList[1].AC, 0.001f);

        // Air gap behind => no structural bonus donated forward.
        var gapped = new Scheme([ArmorLayer.WoodBeam, ArmorLayer.Air, ArmorLayer.WoodBeam]);
        Assert.Equal(8f, gapped.LayerList[0].AC, 0.001f);
    }

    [Fact]
    public void MinPenVelocity_is_the_threshold_where_KD_meets_requirement()
    {
        var scheme = new Scheme([ArmorLayer.MetalBeam, ArmorLayer.MetalBeam]);
        var shell = new Shell(200f, ApsModule.APHead, null, Body(4), 10, 0, 0f);
        shell.EvaluateKinetic(scheme, new TestConditions());

        float vMin = shell.MinVelocityToPenetrate(scheme, 45f);
        Assert.True(vMin > 0f && !float.IsInfinity(vMin));

        // At the solved velocity, KD should equal the requirement (within tolerance).
        float alpha = shell.OverallArmorPierceModifier * 0.0175f;
        float beta = shell.GaugeMultiplier * shell.EffectiveProjectileModuleCount
                     * shell.OverallKineticDamageModifier * 0.16f * Shell.ApsModifier
                     * MathF.Pow(500f / MathF.Max(shell.Gauge, 100f), 0.15f);
        float apAtV = alpha * vMin;
        float kdAtV = beta * vMin;
        float required = scheme.GetRequiredKD(apAtV, 45f, shellIsSabotHead: false);
        Assert.Equal(required, kdAtV, required * 0.02f);
    }

    [Fact]
    public async Task Module_recommends_a_penetrating_config_for_a_metal_stack()
    {
        var module = new ApsShellModule();
        // The module editor normally resolves the targetArmor ModuleReference to the referenced
        // armor module's raw Values dict before compute. In tests we build the dict directly.
        var armorValues = new Dictionary<string, object?>
        {
            ["targetArmor"] = new List<string> { ArmorLayer.MetalBeam.Name, ArmorLayer.MetalBeam.Name },
        };
        var inputs = new ParameterValues()
            .Set("targetArmor", armorValues)
            .Set("optimizeFor", "DPS per cost")
            .Set("minGauge", 100.0).Set("maxGauge", 500.0)
            .Set("maxLoaderLength", 4).Set("impactAngle", 45.0).Set("allowRail", false);

        var result = await module.ComputeAsync(inputs, CancellationToken.None);

        Assert.True(result.Summary.ContainsKey("Recommended shell"));
        var table = Assert.Single(result.Tables);
        Assert.NotEmpty(table.Rows);
    }

    [Fact]
    public void GunpowderHE_442mm_matches_spreadsheet_raw_and_applied()
    {
        // Spreadsheet "Gunpowder HE / No Base / High Velocity / 3 m loader": 442 mm, HE head,
        // 2 HE bodies (+1 from the HE head = 3 effective) → Raw HE 6126, radius 13.68, HE dmg 65728.
        var body = new float[ApsModule.BodyModules.Length];
        body[ApsModule.HEBodyIndex] = 2;
        var shell = new Shell(442f, ApsModule.HEHead, null, body, gpCasingCount: 4, rgCasingCount: 0, railDraw: 0f);
        shell.Evaluate(ApsDamageType.HE, new Scheme(), new TestConditions());

        Assert.Equal(6126f, shell.RawHE, 3f);              // sheet: 6126.21
        Assert.Equal(13.68f, shell.HEExplosionRadius, 0.02f); // sheet: 13.682
        Assert.Equal(65728f, shell.HEDamage, 60f);         // sheet: 65728.51
    }

    [Fact]
    public void HeatDamage_matches_game_shaped_charge_frag_equivalent()
    {
        // Game ShellModel_ExplosiveCharges.DeriveHighEnergyPotential:
        //   SCFE = M_v · (0.8 + HE bodies) · explosiveMod · BaseFragDamage(69000) · HeatHeshMult(1.15)
        //   jet damage = SCFE · HeatFragFraction(0.42) / sqrt(penCoeff, default 0.5)
        var body = new float[ApsModule.BodyModules.Length];
        body[ApsModule.HEBodyIndex] = 3;
        var shell = new Shell(300f, ApsModule.ShapedChargeHead, null, body, gpCasingCount: 4, rgCasingCount: 0, railDraw: 0f);
        shell.Evaluate(ApsDamageType.HEAT, new Scheme(), new TestConditions());

        float mv = MathF.Pow(300f / 500f, 1.8f);
        float scfe = mv * (0.8f + 3f) * 1.0f * (3000f * Shell.ApsModifier) * 1.15f;
        float expected = scfe * 0.42f / MathF.Sqrt(0.5f);

        Assert.Equal(expected, shell.HeatDamage, expected * 0.001f);
        // Sanity: not the old ApsCalc coefficient (957.6435·23·(bodies+0.8)·M_v).
        Assert.NotEqual(mv * 3.8f * Shell.ApsModifier * 957.6435f, shell.HeatDamage, 1f);
    }

    [Fact]
    public void HeatDamage_is_zero_without_a_shaped_charge_head()
    {
        var body = new float[ApsModule.BodyModules.Length];
        body[ApsModule.HEBodyIndex] = 3;
        var shell = new Shell(300f, ApsModule.APHead, null, body, gpCasingCount: 4, rgCasingCount: 0, railDraw: 0f);
        shell.Evaluate(ApsDamageType.HEAT, new Scheme(), new TestConditions());

        Assert.Equal(0f, shell.HeatDamage);
    }

    [Fact]
    public void SabotBody_chem_penalty_cuts_HE_via_chem_modifier()
    {
        // A sabot body (ChemMod 0.25) present should drag the shell chem modifier down and reduce HE.
        var withoutSabot = new float[ApsModule.BodyModules.Length];
        withoutSabot[ApsModule.HEBodyIndex] = 2;
        var clean = new Shell(300f, ApsModule.HEHead, null, withoutSabot, 4, 0, 0f);
        clean.Evaluate(ApsDamageType.HE, new Scheme(), new TestConditions());

        var withSabot = new float[ApsModule.BodyModules.Length];
        withSabot[ApsModule.HEBodyIndex] = 2;
        withSabot[ApsModule.SabotBodyIndex] = 1;
        var tainted = new Shell(300f, ApsModule.HEHead, null, withSabot, 4, 0, 0f);
        tainted.Evaluate(ApsDamageType.HE, new Scheme(), new TestConditions());

        Assert.Equal(0.25f, tainted.OverallChemModifier, 0.001f);
        Assert.True(tainted.HEDamage < clean.HEDamage);
    }

    [Fact]
    public async Task Module_optimizes_HE_against_a_light_target()
    {
        var module = new ApsShellModule();
        var armorValues = new Dictionary<string, object?>
        {
            ["targetArmor"] = new List<string> { ArmorLayer.WoodBeam.Name },
        };
        var inputs = new ParameterValues()
            .Set("targetArmor", armorValues)
            .Set("damageType", "HE")
            .Set("optimizeFor", "DPS per cost")
            .Set("minGauge", 100.0).Set("maxGauge", 500.0)
            .Set("maxLoaderLength", 4).Set("impactAngle", 45.0).Set("allowRail", false);

        var result = await module.ComputeAsync(inputs, CancellationToken.None);

        Assert.True(result.Summary.ContainsKey("HE DPS"));
        var table = Assert.Single(result.Tables);
        Assert.NotEmpty(table.Rows);
    }

    [Fact]
    public void AddApsModule_registers_discoverable_module()
    {
        var provider = new ServiceCollection().AddApsModule().BuildServiceProvider();
        var module = Assert.Single(provider.GetServices<ICalculationModule>());
        Assert.Equal(ApsShellModule.ModuleId, module.Id);
    }
}
