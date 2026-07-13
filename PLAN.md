# FTD Optima — Framework Scaffold Plan (Uno Platform)

> Build environment: **develop in WSL** (dotnet SDK is installed there, not on the Windows host).
> Reopen this repo from WSL and run all `dotnet` commands there.

## Context

`ftd-optima` will become a desktop app that answers *From The Depths* design questions by
running **independent calculator modules**, each turning input parameters into an optimal
answer. Today the repo holds only `APS Shell Configurations.xlsx` (AoKishuba's MIT-licensed
ApsCalc output) and this plan.

This first pass builds the **framework**: a pure-.NET calculation core with a module contract,
a DI/host composition root, and an Uno Platform shell UI that discovers modules and renders a
generic input form + results view. It is proven end-to-end with **one trivial demo module**;
the real APS optimizer port (from ApsCalc's `Shell.cs`/`ShellCalc.cs`, which are already UI-free)
is a deliberate follow-up so we don't conflate framework vs domain concerns.

**Decisions (confirmed):** Uno Platform · in-solution modules wired via
`Microsoft.Extensions.DependencyInjection` · framework + demo module only.

## Prerequisites (run in WSL)

dotnet SDK is already installed in WSL. Confirm and add Uno tooling:

1. `dotnet --version` and `dotnet --list-sdks` — confirm a .NET 9 SDK is present (install the
   .NET 9 SDK in WSL if not; avoid the .NET 10 preview for now).
2. `dotnet tool install --global Uno.Check` then `uno-check --target skia-desktop --target wasm`
   (installs only the workloads we need; skips Android/iOS). Ensure `~/.dotnet/tools` is on PATH.
3. `dotnet new install Uno.Templates`
4. `git init` in the project root; add a standard .NET `.gitignore`.

Development is CLI-based. Build with `dotnet build`, run the **Skia.Desktop** head with
`dotnet run`. GUI from WSL needs WSLg (Win11 has it) — otherwise build/test headless in WSL and
run the desktop head from a Windows dotnet install later.

## Solution structure

```
FtdOptima.sln
src/
  FtdOptima.Core/            net9.0 class library — NO UI deps (the framework contracts)
  FtdOptima.Modules.Demo/    net9.0 — a trivial demo ICalculationModule
  FtdOptima.App/             Uno single-project app (host shell); references Core + Demo
tests/
  FtdOptima.Core.Tests/      xUnit — tests Core + Demo module
```

Rationale: `FtdOptima.Core` stays pure .NET so the calc engine is UI-framework-agnostic,
fast to unit-test, and (later) drops unchanged into an Uno WASM head. UI project contains **no
calculation logic**.

## FtdOptima.Core — the framework contracts (the heart of this pass)

Schema-driven so the shell can render forms for any module without knowing it:

- `ParameterKind` enum: `Number, Integer, Enum, Boolean, Text`.
- `ParameterDescriptor` (record): `Key, Label, ParameterKind Kind, object? Default, double? Min,
  double? Max, IReadOnlyList<string>? Options, string? Unit, string? Help`.
- `ModuleSchema` — `IReadOnlyList<ParameterDescriptor> Parameters`.
- `ParameterValues` — dictionary wrapper with typed getters (`GetDouble/GetInt/GetEnum/GetBool/GetString`)
  and validation against the schema.
- `ResultTable` — `IReadOnlyList<string> Columns` + `IReadOnlyList<IReadOnlyList<object>> Rows`
  (flexible enough for the APS per-loader-size table later).
- `CalculationResult` — `IReadOnlyDictionary<string,object> Summary` (scalar KPIs) +
  `IReadOnlyList<ResultTable> Tables` + optional `string? Notes`.
- `ICalculationModule`:
  ```csharp
  string Id { get; }            // stable, e.g. "demo.kinetic-energy"
  string Name { get; }
  string Description { get; }
  ModuleSchema InputSchema { get; }
  Task<CalculationResult> ComputeAsync(ParameterValues inputs, CancellationToken ct);
  ```
- `ServiceCollectionExtensions.AddFtdModules(this IServiceCollection)` — one call the app uses to
  register all modules as `ICalculationModule` (each module lib also exposes its own
  `AddXxxModule()` so registration stays local to the module).

## FtdOptima.Modules.Demo — proves the plumbing

One clearly-labelled demo: **"Demo: Muzzle Kinetic Energy"**. Inputs exercise every renderer path:
`mass` (Number, kg), `velocity` (Number, m/s), `material` (Enum, 3 options) → returns a `Summary`
(KE = ½·m·v², plus the enum echoed) and a small `ResultTable` (KE at v, 2v, 3v) to exercise the
table view. Registered via `AddDemoModule()`.

## FtdOptima.App — Uno shell (host)

- Scaffold with `dotnet new unoapp` using the **Recommended preset** (Extensions.Hosting + DI +
  Navigation), **Skia** renderer, **MVVM via CommunityToolkit.Mvvm** (safer/larger doc corpus than
  MVUX for a simple forms-and-tables tool).
- Composition root registers `AddFtdModules()`; the shell view model receives
  `IEnumerable<ICalculationModule>`.
- UI: a `NavigationView` (or master/detail) listing discovered modules by `Name`. Selecting one:
  - renders a **generic parameter form** from `InputSchema` (an `ItemsControl` + a
    `DataTemplateSelector` keyed on `ParameterKind`: NumberBox / ComboBox / ToggleSwitch / TextBox),
  - a **Compute** button calling `ComputeAsync`,
  - a **results view**: KPI cards from `Summary` + a table control per `ResultTable`.
- Trim the multi-head noise to Windows/Skia.Desktop (+ keep the WASM head, since web delivery was
  the reason for choosing Uno).

Representative files to create: `App.xaml.cs` (host/DI wiring), `Presentation/MainViewModel.cs`,
`Presentation/ModuleRunnerViewModel.cs`, `Presentation/MainPage.xaml`,
`Presentation/ParameterTemplateSelector.cs`.

## Verification

1. `dotnet build FtdOptima.sln` — solution compiles.
2. `dotnet test` — Core/Demo unit tests pass: schema validation, `ParameterValues` typed getters,
   and Demo `ComputeAsync` returns KE = ½·m·v² for known inputs.
3. `dotnet run --project src/FtdOptima.App` (Skia.Desktop head) — app launches; the Demo module
   appears in the nav list; entering mass/velocity + picking the enum and clicking Compute shows the
   KE summary + the 3-row table. This confirms discovery → generic form render → compute → results
   end-to-end.

## Explicitly out of scope (follow-ups)

- Porting the real ApsCalc optimizer (MIT, https://github.com/AoKishuba/ApsCalcUI) into
  `FtdOptima.Modules.Aps`. Its calc core (`Shell.cs`, `ShellCalc.cs`, `Module.cs`, `Layer.cs`,
  `Scheme.cs`) is UI-free: brute-force exhaustive search + binary search on rail draw.
- WASM head polish / web deployment.
- Reading/importing the existing `APS Shell Configurations.xlsx` as reference data.
