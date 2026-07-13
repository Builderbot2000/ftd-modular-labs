namespace FtdOptima.Modules.Aps.Model;

/// <summary>
/// A concrete APS shell configuration and its computed kinetic performance. Faithful port of the
/// kinetic path of ApsCalc's <c>Shell.cs</c> (AoKishuba/ApsCalcUI, MIT); every constant was verified
/// verbatim against source. Hand-checked against the "APS Shell Configurations" spreadsheet
/// (e.g. gunpowder 100 mm / 7 GP casings / 2 solid bodies / heavy head → V 1123, AP 19.7, KD 1525).
/// </summary>
public sealed class Shell
{
    /// <summary>Global damage scale factor applied to every damage number (Shell.cs: ApsModifier = 23).</summary>
    public const float ApsModifier = 23f;

    public Shell(float gauge, ApsModule headModule, ApsModule? baseModule, float[] bodyModuleCounts,
        float gpCasingCount, float rgCasingCount, float railDraw)
    {
        Gauge = gauge;
        GaugeMultiplier = CalculateGaugeMultiplier(gauge);
        HeadModule = headModule;
        BaseModule = baseModule;
        BodyModuleCounts = bodyModuleCounts;
        GPCasingCount = gpCasingCount;
        RGCasingCount = rgCasingCount;
        RailDraw = railDraw;
    }

    // ---- Configuration -----------------------------------------------------------------------
    public float Gauge { get; }
    public float GaugeMultiplier { get; }
    public ApsModule HeadModule { get; }
    public ApsModule? BaseModule { get; }

    /// <summary>Counts of each body module, indexed by <see cref="ApsModule.BodyModules"/> order.</summary>
    public float[] BodyModuleCounts { get; }
    public float GPCasingCount { get; }
    public float RGCasingCount { get; }
    public float RailDraw { get; set; }

    // ---- Geometry ----------------------------------------------------------------------------
    public float CasingLength { get; private set; }
    public float ProjectileLength { get; private set; }
    public float BodyLength { get; private set; }
    public float TotalLength { get; private set; }
    public float LengthDifferential { get; private set; }
    public float EffectiveBodyLength { get; private set; }
    public float EffectiveProjectileModuleCount { get; private set; }

    // ---- Modifiers ---------------------------------------------------------------------------
    public float OverallVelocityModifier { get; private set; }
    public float OverallKineticDamageModifier { get; private set; }
    public float OverallArmorPierceModifier { get; private set; }
    public float OverallChemModifier { get; private set; }

    // ---- Recoil / velocity -------------------------------------------------------------------
    private float GPRecoilPerCasing => 2500f * GaugeMultiplier;
    private float DrawPerProjectileModule => 12500f * GaugeMultiplier;
    private const float RGCasingDrawMultiplier = 1.25f;
    private const float RGCasingFeltRecoilMultiplier = 0.6f;

    public float GPRecoil { get; private set; }
    public float MaxDrawCasing { get; private set; }
    public float MaxDrawShell { get; private set; }
    public float FeltRecoil { get; private set; }
    public float TotalRecoil { get; private set; }
    public float Velocity { get; private set; }
    public float EffectiveRange { get; private set; }

    // ---- Kinetic damage ----------------------------------------------------------------------
    public float RawKD { get; private set; }
    public float ArmorPierce { get; private set; }
    public float SabotAngleMultiplier { get; set; }
    public float NonSabotAngleMultiplier { get; set; }

    // ---- Reload ------------------------------------------------------------------------------
    public float ShellReloadTime { get; private set; }
    public float ClusterReloadTime { get; private set; }
    public float Uptime { get; private set; }
    public float CooldownTime { get; private set; }

    // ---- Cost / volume -----------------------------------------------------------------------
    public float LoaderVolume { get; private set; }
    public float LoaderCost { get; private set; }
    public float RecoilVolume { get; private set; }
    public float RecoilCost { get; private set; }
    public float CoolerVolume { get; private set; }
    public float CoolerCost { get; private set; }
    public float ChargerVolume { get; private set; }
    public float ChargerCost { get; private set; }
    public float EngineVolume { get; private set; }
    public float EngineCost { get; private set; }
    public float FuelBurned { get; private set; }
    public float FuelAccessVolume { get; private set; }
    public float FuelAccessCost { get; private set; }
    public float FuelStorageVolume { get; private set; }
    public float FuelStorageCost { get; private set; }
    public float CostPerShell { get; private set; }
    public float AmmoUsed { get; private set; }
    public float AmmoAccessVolume { get; private set; }
    public float AmmoAccessCost { get; private set; }
    public float AmmoStorageVolume { get; private set; }
    public float AmmoStorageCost { get; private set; }
    public float VolumePerLoader { get; private set; }
    public float CostPerLoader { get; private set; }

    // ---- Kinetic results ---------------------------------------------------------------------
    public float KineticDamage { get; private set; }
    public float KineticDps { get; private set; }
    public float KineticDpsPerVolume { get; private set; }
    public float KineticDpsPerCost { get; private set; }

    // ---- Chemical payload results ------------------------------------------------------------
    public float RawHE { get; private set; }
    public float HEExplosionRadius { get; private set; }
    public float HEDamage { get; private set; }
    public float HeatDamage { get; private set; }
    public float FragDamage { get; private set; }
    public float FragCount { get; private set; }
    public float EmpDamage { get; private set; }
    public float MdDamage { get; private set; }
    public float IncendiaryDamage { get; private set; }

    // ---- Selected-type results (what the optimizer ranks) ------------------------------------
    public ApsDamageType DamageType { get; private set; }
    public float PrimaryDamage { get; private set; }
    public float Dps { get; private set; }
    public float DpsPerVolume { get; private set; }
    public float DpsPerCost { get; private set; }

    /// <summary>True if the shell penetrated the target scheme (else all DPS values are 0).</summary>
    public bool Penetrates { get; private set; }

    public static float CalculateGaugeMultiplier(float gauge) => MathF.Pow(gauge / 500f, 1.8f);

    /// <summary>Convenience wrapper: evaluate the kinetic damage type.</summary>
    public void EvaluateKinetic(Scheme targetScheme, TestConditions conditions) =>
        Evaluate(ApsDamageType.Kinetic, targetScheme, conditions);

    /// <summary>
    /// Runs the full evaluation of the given damage type against a target armor scheme under the
    /// given conditions, populating velocity/AP/KD, reload, cost/volume, the payload damage, and the
    /// DPS metrics used for ranking. Kinetic stats are always computed (the penetration gate needs them).
    /// </summary>
    public void Evaluate(ApsDamageType dt, Scheme targetScheme, TestConditions conditions)
    {
        DamageType = dt;
        CalculateLengths();
        CalculateVelocityModifier();
        CalculateKineticDamageModifier();
        CalculateArmorPierceModifier();

        CalculateMaxDraw();
        RailDraw = MathF.Min(RailDraw, MaxDrawShell);
        CalculateRecoil();

        CalculateReloadTime(conditions);
        CalculateCooldownTime();
        CalculateLoaderVolumeAndCost(conditions);
        CalculateCoolerVolumeAndCost(conditions);

        CalculateVelocity();
        CalculateEffectiveRange();

        CalculateRailVolumeAndCost(conditions);
        CalculateRecoilVolumeAndCost(conditions);
        CalculateVariableVolumesAndCosts(conditions);
        CalculateVolumeAndCostPerLoader();

        CalculateKineticDamageRaw();
        CalculateAP();
        CalculateChemModifier();
        CalculateChemDamage(dt);

        // Angle multipliers use the front layer's slope (matches ApsCalc's ParameterInput).
        float frontBaseAngle = targetScheme.LayerList.Count > 0 ? targetScheme.LayerList[0].BaseAngle : 0f;
        SabotAngleMultiplier = MathF.Abs(MathF.Cos((conditions.ImpactAngle + frontBaseAngle) * MathF.PI / 240f));
        NonSabotAngleMultiplier = MathF.Abs(MathF.Cos((conditions.ImpactAngle + frontBaseAngle) * MathF.PI / 180f));

        // Representative AC for damage scaling = effective AC of the first layer hit.
        float targetAC = targetScheme.LayerList.Count > 0 ? targetScheme.LayerList[0].AC : 0f;

        bool pens = RawKD >= targetScheme.GetRequiredKD(ArmorPierce, conditions.ImpactAngle, HeadModule == ApsModule.SabotHead)
            || (HeadModule == ApsModule.HollowPoint && RawKD >= targetScheme.GetRequiredThump(ArmorPierce));
        Penetrates = pens || targetScheme.LayerList.Count == 0;

        if (Penetrates)
        {
            CalculateSelectedDps(dt, targetAC);
        }
        else
        {
            KineticDamage = KineticDps = KineticDpsPerVolume = KineticDpsPerCost = 0f;
            PrimaryDamage = Dps = DpsPerVolume = DpsPerCost = 0f;
        }
    }

    private void CalculateLengths()
    {
        BodyLength = 0f;
        if (BaseModule != null)
        {
            BodyLength += MathF.Min(Gauge, BaseModule.MaxLength);
        }
        for (int i = 0; i < BodyModuleCounts.Length; i++)
        {
            BodyLength += BodyModuleCounts[i] * MathF.Min(Gauge, ApsModule.BodyModules[i].MaxLength);
        }

        CasingLength = (GPCasingCount + RGCasingCount) * Gauge;
        float headLength = MathF.Min(Gauge, HeadModule.MaxLength);
        ProjectileLength = BodyLength + headLength;
        TotalLength = CasingLength + ProjectileLength;

        LengthDifferential = MathF.Max(2f * Gauge - BodyLength, 0f);
        EffectiveBodyLength = MathF.Max(2f * Gauge, BodyLength);
        EffectiveProjectileModuleCount = ProjectileLength / Gauge;
    }

    private void CalculateVelocityModifier()
    {
        float weighted = 0f;
        if (BaseModule != null)
        {
            weighted += BaseModule.VelocityMod * MathF.Min(Gauge, BaseModule.MaxLength);
        }
        for (int i = 0; i < BodyModuleCounts.Length; i++)
        {
            weighted += MathF.Min(Gauge, ApsModule.BodyModules[i].MaxLength) * ApsModule.BodyModules[i].VelocityMod * BodyModuleCounts[i];
        }
        if (LengthDifferential > 0f)
        {
            weighted += 0.7f * LengthDifferential; // ghost module penalizing short shells
        }
        weighted /= EffectiveBodyLength;

        OverallVelocityModifier = weighted * HeadModule.VelocityMod;
        if (BaseModule == ApsModule.BaseBleeder)
        {
            OverallVelocityModifier += 0.15f;
        }
    }

    private void CalculateKineticDamageModifier()
    {
        float weighted = 0f;
        if (BaseModule != null)
        {
            weighted += BaseModule.KineticDamageMod * MathF.Min(Gauge, BaseModule.MaxLength);
        }
        for (int i = 0; i < BodyModuleCounts.Length; i++)
        {
            weighted += MathF.Min(Gauge, ApsModule.BodyModules[i].MaxLength) * ApsModule.BodyModules[i].KineticDamageMod * BodyModuleCounts[i];
        }
        if (LengthDifferential > 0f)
        {
            weighted += LengthDifferential;
        }
        weighted /= EffectiveBodyLength;

        OverallKineticDamageModifier = weighted * HeadModule.KineticDamageMod;
        if (BaseModule == ApsModule.GravRam)
        {
            OverallKineticDamageModifier *= 0.7f;
        }
    }

    private void CalculateArmorPierceModifier()
    {
        float weighted = 0f;
        if (BaseModule != null)
        {
            weighted += BaseModule.ArmorPierceMod * MathF.Min(Gauge, BaseModule.MaxLength);
        }
        for (int i = 0; i < BodyModuleCounts.Length; i++)
        {
            weighted += MathF.Min(Gauge, ApsModule.BodyModules[i].MaxLength) * ApsModule.BodyModules[i].ArmorPierceMod * BodyModuleCounts[i];
        }
        if (LengthDifferential > 0f)
        {
            weighted += LengthDifferential;
        }
        weighted /= EffectiveBodyLength;

        OverallArmorPierceModifier = weighted * HeadModule.ArmorPierceMod;
    }

    private void CalculateMaxDraw()
    {
        MaxDrawCasing = DrawPerProjectileModule * RGCasingDrawMultiplier * RGCasingCount;
        float maxDrawProjectile = DrawPerProjectileModule * EffectiveProjectileModuleCount;
        MaxDrawShell = MaxDrawCasing + maxDrawProjectile;
    }

    private void CalculateRecoil()
    {
        GPRecoil = GPRecoilPerCasing * GPCasingCount;
        FeltRecoil = GPRecoil + RGCasingFeltRecoilMultiplier * MathF.Min(RailDraw, MaxDrawCasing) + MathF.Max(RailDraw - MaxDrawCasing, 0f);
        TotalRecoil = GPRecoil + RailDraw;
    }

    private void CalculateVelocity()
    {
        Velocity = MathF.Sqrt(TotalRecoil * 85f * Gauge / (GaugeMultiplier * ProjectileLength)) * OverallVelocityModifier;
    }

    private void CalculateEffectiveRange()
    {
        float gravComp = BodyModuleCounts[ApsModule.GravCompensatorIndex];
        float effectiveTime = 10f * OverallVelocityModifier * (ProjectileLength / 1000f) * (1f + gravComp);
        EffectiveRange = Velocity * effectiveTime;
    }

    private void CalculateReloadTime(TestConditions conditions)
    {
        ShellReloadTime = MathF.Pow(Gauge / 500f, 1.35f)
            * (2f + EffectiveProjectileModuleCount + 0.25f * (RGCasingCount + GPCasingCount))
            * 17.5f;

        // Regular (non-belt, non-DIF) loader. Belt/DIF are follow-ups.
        ClusterReloadTime = ShellReloadTime / (1f + conditions.RegularClipsPerLoader);

        float capacityModifier = Gauge <= 250f ? 2f : 1f;
        float shellCapacity = conditions.RegularClipsPerLoader * MathF.Min(64f, MathF.Floor(1000f / Gauge) * capacityModifier) + 1f;
        float timeToEmptySeconds =
            shellCapacity * ClusterReloadTime * (1f + conditions.RegularClipsPerLoader)
            / (1f + conditions.RegularClipsPerLoader - conditions.RegularInputsPerLoader);

        if (timeToEmptySeconds >= conditions.TestIntervalSeconds || conditions.RegularInputsPerLoader >= 1f + conditions.RegularClipsPerLoader)
        {
            Uptime = 1f;
        }
        else
        {
            float reloadWhenEmpty = ClusterReloadTime * (1f + conditions.RegularClipsPerLoader) / conditions.RegularInputsPerLoader;
            float reducedRofDuration = MathF.Max(0f, conditions.TestIntervalSeconds - timeToEmptySeconds);
            Uptime = (timeToEmptySeconds + ClusterReloadTime / reloadWhenEmpty * reducedRofDuration) / conditions.TestIntervalSeconds;
        }
    }

    private void CalculateCooldownTime()
    {
        CooldownTime = MathF.Max(0f,
            3.75f * GaugeMultiplier
            / MathF.Pow(Gauge * Gauge * Gauge / 125_000_000f, 0.15f)
            * 17.5f
            * MathF.Pow(GPCasingCount, 0.35f)
            / 2f);
    }

    private void CalculateLoaderVolumeAndCost(TestConditions conditions)
    {
        float clips = conditions.RegularClipsPerLoader;
        float inputs = conditions.RegularInputsPerLoader;

        // Loader volume/cost scales in 1 m brackets by total shell length (single loader).
        // Bracket b≥2: base = 240+30b, clip coeff = 160+20b. The 1 m bracket is a special base.
        int lengthBracket = Math.Clamp((int)MathF.Ceiling(TotalLength / 1000f), 1, 8);
        LoaderVolume = lengthBracket * (1f + clips) + inputs;
        LoaderCost = lengthBracket == 1
            ? 240f + 160f * clips + 50f * inputs
            : (240f + 30f * lengthBracket) + (160f + 20f * lengthBracket) * clips + 50f * inputs;
    }

    private void CalculateCoolerVolumeAndCost(TestConditions conditions)
    {
        if (GPCasingCount > 0f)
        {
            CoolerVolume = CooldownTime * MathF.Sqrt(Gauge / 1000f) / ClusterReloadTime
                / (1f + conditions.BarrelCount * 0.05f) / 0.176775f;
            CoolerCost = CoolerVolume * 50f;
        }
        else
        {
            CoolerVolume = 0f;
            CoolerCost = 0f;
        }
    }

    private void CalculateRecoilVolumeAndCost(TestConditions conditions)
    {
        if (conditions.GunUsesRecoilAbsorbers)
        {
            RecoilVolume = FeltRecoil / (ClusterReloadTime * 120f);
            RecoilCost = RecoilVolume * 80f;
        }
        else
        {
            RecoilVolume = 0f;
            RecoilCost = 0f;
        }
    }

    private void CalculateRailVolumeAndCost(TestConditions conditions)
    {
        if (RailDraw > 0f)
        {
            float drawPerSecond = RailDraw / ClusterReloadTime;
            ChargerVolume = drawPerSecond / 280f;
            ChargerCost = ChargerVolume * 560f;
            EngineVolume = drawPerSecond / conditions.EnginePpv;
            EngineCost = drawPerSecond / conditions.EnginePpc;
            FuelBurned = drawPerSecond * conditions.TestIntervalSeconds / conditions.EnginePpm * Uptime;

            float fuelStorageNeeded;
            if (conditions.UsesSpecialFuel)
            {
                float fuelAccessNeeded = drawPerSecond / conditions.EnginePpm;
                FuelAccessVolume = fuelAccessNeeded * 1.2f;
                FuelAccessCost = FuelAccessVolume * 10f;
                fuelStorageNeeded = drawPerSecond * MathF.Max(conditions.TestIntervalSeconds - 600f, 0f) / conditions.EnginePpm;
            }
            else
            {
                FuelAccessVolume = 0f;
                FuelAccessCost = 0f;
                fuelStorageNeeded = drawPerSecond * conditions.TestIntervalSeconds / conditions.EnginePpm;
            }
            FuelStorageVolume = fuelStorageNeeded / conditions.StoragePerVolume * Uptime;
            FuelStorageCost = fuelStorageNeeded / conditions.StoragePerCost * Uptime;
        }
        else
        {
            ChargerVolume = ChargerCost = EngineVolume = EngineCost = FuelBurned = 0f;
            FuelAccessVolume = FuelAccessCost = FuelStorageVolume = FuelStorageCost = 0f;
        }
    }

    private void CalculateVariableVolumesAndCosts(TestConditions conditions)
    {
        CostPerShell = (EffectiveProjectileModuleCount + GPCasingCount * 0.25f + RGCasingCount * 0.15f) * 5f * GaugeMultiplier;
        AmmoUsed = CostPerShell * conditions.TestIntervalSeconds / ClusterReloadTime * Uptime;

        float shellCostPerMinute = CostPerShell / ClusterReloadTime * 60f;
        AmmoAccessVolume = shellCostPerMinute / 50f;
        AmmoAccessCost = shellCostPerMinute / 5f;

        float ammoStorageNeeded = CostPerShell * MathF.Max(conditions.TestIntervalSeconds - 600f, 0f) / ClusterReloadTime * Uptime;
        AmmoStorageVolume = ammoStorageNeeded / conditions.StoragePerVolume;
        AmmoStorageCost = ammoStorageNeeded / conditions.StoragePerCost;
    }

    private void CalculateVolumeAndCostPerLoader()
    {
        VolumePerLoader = LoaderVolume + RecoilVolume + CoolerVolume + ChargerVolume
            + AmmoAccessVolume + AmmoStorageVolume + EngineVolume + FuelAccessVolume + FuelStorageVolume;
        CostPerLoader = LoaderCost + RecoilCost + CoolerCost + ChargerCost + AmmoUsed
            + AmmoAccessCost + AmmoStorageCost + FuelBurned + EngineCost + FuelAccessCost + FuelStorageCost;
    }

    private void CalculateKineticDamageRaw()
    {
        float smallGauge = HeadModule == ApsModule.HollowPoint ? 1f : MathF.Pow(500f / MathF.Max(Gauge, 100f), 0.15f);
        RawKD = smallGauge * GaugeMultiplier * EffectiveProjectileModuleCount * Velocity
            * OverallKineticDamageModifier * 0.16f * ApsModifier;
    }

    private void CalculateAP()
    {
        ArmorPierce = Velocity * OverallArmorPierceModifier * 0.0175f;
    }

    private void CalculateKineticDps(float targetAC)
    {
        float pierceFactor = MathF.Min(1f, targetAC > 0f ? ArmorPierce / targetAC : 1f);
        if (HeadModule == ApsModule.HollowPoint || targetAC == 20f)
        {
            KineticDamage = RawKD * pierceFactor;
        }
        else if (HeadModule == ApsModule.SabotHead)
        {
            KineticDamage = RawKD * pierceFactor * SabotAngleMultiplier;
        }
        else
        {
            KineticDamage = RawKD * pierceFactor * NonSabotAngleMultiplier;
        }

        KineticDps = KineticDamage / ClusterReloadTime * Uptime;
        KineticDpsPerVolume = VolumePerLoader > 0f ? KineticDps / VolumePerLoader : 0f;
        KineticDpsPerCost = CostPerLoader > 0f ? KineticDps / CostPerLoader : 0f;
    }

    /// <summary>Chem payload multiplier: min over base/body/head chem mods; Disruptor stacks ×0.5.</summary>
    private void CalculateChemModifier()
    {
        OverallChemModifier = 1f;
        if (BaseModule != null)
        {
            OverallChemModifier = MathF.Min(OverallChemModifier, BaseModule.ChemMod);
        }
        for (int i = 0; i < BodyModuleCounts.Length; i++)
        {
            if (BodyModuleCounts[i] > 0f)
            {
                OverallChemModifier = MathF.Min(OverallChemModifier, ApsModule.BodyModules[i].ChemMod);
            }
        }
        if (HeadModule == ApsModule.Disruptor)
        {
            OverallChemModifier *= 0.5f;
        }
        else
        {
            OverallChemModifier = MathF.Min(OverallChemModifier, HeadModule.ChemMod);
        }
    }

    /// <summary>Computes the raw payload damage for the selected chem type (verbatim ApsCalc formulas).</summary>
    private void CalculateChemDamage(ApsDamageType dt)
    {
        switch (dt)
        {
            case ApsDamageType.HE:
            {
                float heBodies = BodyModuleCounts[ApsModule.HEBodyIndex];
                if (HeadModule == ApsModule.ShapedChargeHead)
                {
                    heBodies += 0.2f;
                }
                else if (HeadModule == ApsModule.HEHead)
                {
                    heBodies++;
                }
                RawHE = 3000f * MathF.Pow(GaugeMultiplier * heBodies * 120f * ApsModifier / 3000f * OverallChemModifier, 0.9f);
                HEExplosionRadius = MathF.Min(MathF.Pow(RawHE, 0.3f), 30f);
                float sphereVolume = MathF.Pow(HEExplosionRadius, 3f) * MathF.PI * 4f / 3f;
                HEDamage = RawHE * sphereVolume / 1000f;
                break;
            }
            case ApsDamageType.HEAT:
            {
                if (HeadModule == ApsModule.ShapedChargeHead)
                {
                    float heBodies = BodyModuleCounts[ApsModule.HEBodyIndex];
                    HeatDamage = GaugeMultiplier * (heBodies + 0.8f) * OverallChemModifier * ApsModifier * 957.6435f;
                }
                else
                {
                    HeatDamage = 0f;
                }
                break;
            }
            case ApsDamageType.Frag:
            {
                float fragBodies = BodyModuleCounts[ApsModule.FragBodyIndex];
                if (HeadModule == ApsModule.FragHead)
                {
                    fragBodies++;
                }
                FragDamage = GaugeMultiplier * fragBodies * OverallChemModifier * 3000f * ApsModifier;
                FragCount = MathF.Floor(MathF.Pow(FragDamage, 0.25f));
                break;
            }
            case ApsDamageType.EMP:
            {
                float empBodies = BodyModuleCounts[ApsModule.EmpBodyIndex];
                if (HeadModule == ApsModule.EmpHead)
                {
                    empBodies++;
                }
                EmpDamage = GaugeMultiplier * empBodies * OverallChemModifier * 75f * ApsModifier;
                break;
            }
            case ApsDamageType.Incendiary:
            {
                float bodies = BodyModuleCounts[ApsModule.IncendiaryBodyIndex];
                if (HeadModule == ApsModule.IncendiaryHead)
                {
                    bodies++;
                }
                IncendiaryDamage = GaugeMultiplier * bodies * OverallChemModifier * 330f * ApsModifier;
                break;
            }
            case ApsDamageType.MD:
            {
                float mdBodies = BodyModuleCounts[ApsModule.MDBodyIndex];
                if (HeadModule == ApsModule.MDHead)
                {
                    mdBodies++;
                }
                MdDamage = 3000f * MathF.Pow(GaugeMultiplier * mdBodies / 31.25f * ApsModifier * OverallChemModifier, 0.9f);
                break;
            }
        }
    }

    /// <summary>Sets <see cref="PrimaryDamage"/> and the DPS metrics for the selected type.</summary>
    private void CalculateSelectedDps(ApsDamageType dt, float targetAC)
    {
        if (dt == ApsDamageType.Kinetic)
        {
            CalculateKineticDps(targetAC);
            PrimaryDamage = KineticDamage;
            Dps = KineticDps;
            DpsPerVolume = KineticDpsPerVolume;
            DpsPerCost = KineticDpsPerCost;
            return;
        }

        PrimaryDamage = dt switch
        {
            ApsDamageType.HE => HEDamage,
            ApsDamageType.HEAT => HeatDamage,
            ApsDamageType.Frag => FragDamage,
            ApsDamageType.EMP => EmpDamage,
            ApsDamageType.Incendiary => IncendiaryDamage,
            ApsDamageType.MD => MdDamage,
            _ => 0f,
        };

        Dps = PrimaryDamage / ClusterReloadTime * Uptime;
        DpsPerVolume = VolumePerLoader > 0f ? Dps / VolumePerLoader : 0f;
        DpsPerCost = CostPerLoader > 0f ? Dps / CostPerLoader : 0f;
    }
}
