# APS port vs. game formulas — reconciliation audit

**Scope:** compare the APS shell math in `src/FtdModularLabs.Modules.Aps` (a port of AoKishuba's
ApsCalc, MIT) against the decompiled **real game** formulas in `references/FtD-APS-Reference`
(`Ftd.dll`, build 2025-12). Goal: find every place the port diverges from the game and align the
design-time shell math to `Ftd.dll`.

**Headline:** the port's core physics is *faithful to the game*. ApsCalc got the ballistics,
kinetic, AP, reload, cooldown, recoil, and most payload formulas right, so the port inherits that
accuracy. Divergences are few and narrow. Three were game-formula errors and are now fixed; the
rest are either scoring heuristics with no game equivalent, or deliberate design choices.

---

## A. Confirmed faithful (port == game)

Each was checked algebraically, accounting for the port working in **mm gauge** while the game works
in **m diameter**. The key identity that makes them line up:

```
GaugeMultiplier = (gauge/500)^1.8  ≡  VolumeToModifier = (π·d³/4 / (π·0.5³/4))^0.6   (M_v)
```

| Quantity | Port | Game reference | Status |
|---|---|---|---|
| Size multiplier | `(gauge/500)^1.8` | `VolumeToModifier` (`ShellConstants`) | ✅ identical |
| Muzzle velocity (GP + rail) | `sqrt(TotalRecoil·85·gauge/(M_v·projLen))·velMod` | `sqrt((E + draw·85)/LV)·SpeedModifier`, `E=212500·M_v·P` | ✅ identical (per-GP-casing propellant = 1) |
| `StandardShellRecoilFactor` | `85` | `5·212500/12500 = 85` | ✅ |
| Reload | `(gauge/500)^1.35·(2+L_mod+0.25·casings)·17.5` | `17.5·(M_v/M_vw)·N`, `M_v/M_vw = (gauge/500)^1.35` | ✅ identical |
| Cooldown | `3.75·(gauge/500)^1.35·17.5·P^0.35/2` | `3.75·Reload·P^0.35/(N·2)` | ✅ identical |
| Raw kinetic damage | `smallGauge·M_v·modCount·v·KDmod·(0.16·23)` | `LV·v·KDmod·3.68·(0.5/d)^0.15` | ✅ identical, incl. `BaseKineticDamage=3.68` and small-gauge factor |
| Armour pierce | `v·APmod·0.0175` | `APmod·v·BaseApPerMs(0.0175)` | ✅ identical |
| Modifier aggregation (speed/KD/AP) | head base × length-weighted body avg, pad to 2 modules, speed filler 0.7 / others 1.0 | `ShellModifierAggregation` | ✅ identical |
| Felt recoil | `GPRecoil + 0.6·min(draw,casing) + max(draw−casing,0)` | `GetRecoil`: `0.6·min + E/85 + max(…)`, `E/85 = GPRecoil` | ✅ identical |
| Chem/explosive modifier | `min(chemMods)`; disruptor ×0.5 | sabot 0.25 / waterpen 0.75 / shieldbuster ×0.5 | ✅ equivalent |
| EMP damage | `M_v·n·chem·(75·23=1725)` | `M_v·n·BaseEmpDamage(1725)·mod` | ✅ identical |
| Frag damage (total) | `M_v·n·chem·(3000·23=69000)` | `M_v·n·BaseFragDamage(69000)·mod` | ✅ identical |
| MD / flak damage | `3000·(M_v·n/31.25·23·chem)^0.9` | `3000·(M_v·n·BaseFlakDamage(2208)·mod/3000)^0.9`, `2208/3000 = 23/31.25` | ✅ identical |
| Incendiary (default) | `M_v·n·chem·(330·23=7590)` | game **fuel** at intensity 0 = `1.2·M_v·n·126500/20 = 7590·M_v·n` | ✅ matches default; ignores intensity/oxidizer tuning |
| Effective range | `10·velMod·(projLen/1000)·(1+gravComp)` | `10·SpeedModifier·LengthOfShell·(1+gravComp)` | ✅ identical |
| Module stat table | per-module vel/KD/AP modifiers | `ShellModuleDictionary` | ✅ all match except one (see B1) |

---

## B. Real divergences from the game

### B1. Incendiary head AP modifier — **FIXED**
`ApsModule.IncendiaryHead` had `armorPierceMod = 0.8`. The game's `FireHead` uses
`HeadWeakAPModifier = 1.0` (`ShellModuleDictionary.cs`, `AdvCannonConstants.HeadWeakAPModifier = 1f`).
The 0.8 under-rated incendiary-head penetration by 20% and could wrongly fail the penetration gate.
→ Changed to `1.0`. (`ApsModule.cs`)

### B2. Rail-casing felt-recoil boundary carried an extra 1.25× — **FIXED**
Port `MaxDrawCasing = 12500·M_v·1.25·RGCount`. The game's `MaxRailCasingDraw` (the boundary in the
felt-recoil formula) is `12500·M_v·casingCapacity` with **no** `RailCasingModifier`; the 1.25 applies
only to the *shell-wide* `MaxRailDraw`. The port over-stated the boundary, so too much rail draw got
the reduced-recoil (0.6×) treatment → understated recoil-absorber cost on rail shells.
→ `MaxDrawCasing` now drops the 1.25; `MaxDrawShell` is unchanged (still `projModules + 1.25·RGCount`).
Only affects rail felt recoil. (`Shell.cs`)

### B3. HEAT raw magnitude was ApsCalc's model, not the game's — **FIXED**
Port had `HeatDamage = M_v·(heBodies+0.8)·chem·(957.6435·23 ≈ 22026)` — a linear coefficient that
corresponds to no game quantity. Replaced with a faithful port of `ShellModel_ExplosiveCharges.
DeriveHighEnergyPotential`:

```
ShapedChargeFragEquivalent = M_v · (0.8 + Σ HeContributionToSpecialHead) · explosiveMod
                                 · BaseFragDamage(69000) · HeatHeshMultiplier(1.15)
HeatDamage (jet damage)    = SCFE · HeatFragFraction(0.42) / sqrt(ShapedChargePenetrationCoefficient)
```

Defaults taken from the game's `ShellModuleParams`: `HeContributionToSpecialHead = 1.0` (full feed —
each HE body contributes 1), and `ShapedChargePenetrationCoefficient = 0.5` (the [0.5, 1] slider's
default, which is also the damage-maximal setting an optimizer would pick). The penetration-depth
metric (`GetHeatPenMetric`) is not scored here — the port gates HEAT through the shared penetration
model, consistent with the other payload types. (`Shell.cs`; covered by
`HeatDamage_matches_game_shaped_charge_frag_equivalent`.)

---

## C. Out of scope — no game formula to reconcile against

The reference is **design-time shell stats only** (it explicitly excludes impact-time damage
resolution and the surrounding turret engineering). These port areas therefore have no `Ftd.dll`
counterpart in the reference and are *not* divergences:

- **Cost / volume model** (`LoaderVolume/Cost`, `Cooler`, `Recoil`, `Charger`, `Engine`, fuel, ammo
  storage/access): ApsCalc's own estimate of the blocks a turret needs. The game computes none of
  this at design time.
- **Payload → single "damage" scalars** (`HEDamage = RawHE·(4/3·π·r³)/1000`, frag/HEAT rollups, the
  HE radius cap at 30 m): ApsCalc scoring heuristics layered on top of the game-accurate *raw* payload
  numbers (`RawHE`, frag total, EMP, MD all reduce to exact game values — see table A). Kept as-is; the
  spreadsheet cross-check tests pin them.
- **Penetration / breach model**: lives in the Armor module; the reference omits impact-time.
- **Objective function**: the game rates shells via `GetEstimatedPower` / `FirepowerHandler`. The port
  deliberately ranks by DPS-per-cost/volume against a *specific target armor scheme*, which is the
  module's stated purpose. Intentional, not a divergence.

---

## Verification

`ApsShellTests` reproduces columns of AoKishuba's "APS Shell Configurations" spreadsheet (the ApsCalc
reference output). The fixed items (B1 incendiary-head AP, B2 rail felt recoil) are not exercised by
those gunpowder-kinetic / HE cases, so all cross-check tests remain green. B3 (HEAT) is now pinned by
`HeatDamage_matches_game_shaped_charge_frag_equivalent`, which independently re-encodes the game
constants. Full suite: 70 tests green.
