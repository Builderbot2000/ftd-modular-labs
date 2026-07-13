# FTD Optima — Vehicle Design Optimizer

A desktop app (Uno Platform / .NET 9) for designing and optimizing vehicles for the game
**From the Depths**. You create and manage **vehicle designs** (e.g. *Valiant-class Battleship*),
each holding a collection of **modules** classed by subsystem type (APS Turret, CRAM Turret, LAMS,
Steam Engine, …). Designs and modules can be created blank or from a **template library** that ships
with built-in presets and is user-manageable. Where a subsystem has a calculator (today: **APS**),
the module editor runs it in place; other subsystem types are still fully nameable, organizable, and
persistable ("calculator coming soon").

## Solution layout

```
FtdOptima.sln
src/
  FtdOptima.Core/            Calculator framework — ICalculationModule, ModuleSchema,
                             ParameterValues, CalculationResult (pure .NET, no UI).
  FtdOptima.Modules.Aps/     The APS shell optimizer (a calculator) + its physics model.
  FtdOptima.Modules.Demo/    A trivial demo calculator.
  FtdOptima.Domain/          Design-management layer (pure .NET, references only Core):
                               Catalog/       SubsystemType + SubsystemCatalog + registry
                               Model/         VehicleDesign, DesignModule, templates, DesignFactory
                               Serialization/ ParameterValueSnapshot, DTOs, JSON options
                               Storage/       repositories, IAppDataPaths, embedded built-in templates
                             Templates/*.json are shipped as embedded resources.
  FtdOptima.App.Presentation/ ParameterField (bindable form field).
  FtdOptima.App/             Uno single-project app (Skia desktop head; WASM opt-in).
tests/
  FtdOptima.Core.Tests/      xUnit — framework, serialization round-trip, repository CRUD,
                             template copy-on-create, registry, and an end-to-end flow test.
```

## Architecture notes

- **Calculators vs. subsystem types vs. modules.** An `ICalculationModule` is a stateless *calculator*
  (schema-in → result-out), discovered via DI and keyed by a stable `Id`. `SubsystemCatalog` lists
  every designable subsystem *type*; a type carries the `Id` of its calculator when one exists.
  A `DesignModule` is a persisted *instance* of a type with a saved parameter-value snapshot.
- **Persistence.** Local JSON files under the per-user app-data folder (`%LOCALAPPDATA%/FtdOptima`
  on Windows, `~/.local/share/FtdOptima` on Linux): one file per design under `designs/`, user
  templates under `templates/`. Built-in templates are embedded resources merged in at read time.
  Storage sits behind `IVehicleDesignRepository` / `ITemplateRepository`, so a WASM key/value store
  can slot in later (the JSON-file store is desktop-first).
- **Values serialization.** `ParameterValueSnapshot` normalizes values by schema kind, surviving both
  boxed UI primitives and deserialized `JsonElement`s — the key to a stable round-trip (incl. the
  `LayerStack` armor builder).
- **Templates are copy-on-create.** Creating a design/module deep-copies the template into a fresh,
  independent entity; built-ins are read-only.

## Build & run (from WSL)

```bash
dotnet build FtdOptima.sln
dotnet test tests/FtdOptima.Core.Tests
dotnet run --project src/FtdOptima.App/FtdOptima.App -f net9.0-desktop   # needs WSLg/display
```

The WebAssembly head is preserved but opt-in (needs the `wasm-tools` workload):
`dotnet build src/FtdOptima.App/FtdOptima.App -p:EnableWasm=true`.
