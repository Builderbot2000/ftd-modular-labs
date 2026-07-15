# From the Depths — Designable Subsystems

A catalog of all player-designable subsystems in From the Depths, used as a roadmap
for which Optima optimization modules to build. Sourced from the official wiki
(fromthedepths.wiki.gg / fandom).

This document mirrors `SubsystemCatalog.All` one-for-one — each bullet is one catalog
entry with its stable slug. Each entry is one distinct FtD module type; different types
are never lumped together. ⭐ marks systems with large discrete design spaces (most
amenable to Optima-style optimization). *(module)* marks types with a calculator wired today.

## Weapons
- **APS Turret** (`weapon.aps`) ⭐ *(module)* — Advanced Projectile System; modular cartridge cannons, the most complex weapon system. Includes the Railgun electric-boost variant and covers CIWS close-in gun roles.
- **CRAM Cannon** (`weapon.cram`) ⭐ — simple arc-firing cannons with pentagonal packing.
- **Missile Launcher** (`weapon.missiles`) ⭐ — guided munitions: missiles, torpedoes, rockets, bombs, mines, depth charges, and interceptors.
- **Laser** (`weapon.lasers`) ⭐ — cavity/pump laser weapons (also drive LAMS defense).
- **Particle Cannon** (`weapon.particle`) ⭐ — beam weapons with tetris-tuned nodes.
- **Flamethrower** (`weapon.flamethrower`) — short-range incendiary area weapon.
- **Plasma Cannon** (`weapon.plasma`) — short-range plasma area weapon.
- **Drill** (`weapon.melee-drill`) — melee drilling weapon.
- **Tactical Nuke** (`weapon.nuke`) — tactical nuclear warhead.
- **Mass Driver** (`weapon.mass-driver`) — high-velocity mass driver.

## Propulsion
- **Custom Jet Engine** (`prop.jet-custom`) — player-built jet engine from intakes, injectors, exhausts, and afterburners.
- **Thruster** (`prop.thruster`) — prebuilt directional thrusters, including ion thrusters.
- **Propeller** (`prop.propeller`) — standard propellers.
- **Huge Propeller** (`prop.propeller-huge`) — huge / large propellers.
- **Wheels & Tracks** (`prop.wheel`) — land propulsion.
- **Helicopter Blade** (`prop.helicopter-blade`) — helicopter blades for rotor lift.
- **Hydrofoil** (`prop.hydrofoil`) — lift-generating hydrofoils.
- **Sail** (`prop.sail`) — wind-driven sails.
- **Paddle** (`prop.paddle`) — paddle propulsion.
- **Fin** (`prop.fin`) — water / air control fins.

## Power & Energy
All feed **Engine Power**, consumed by propulsion, lasers, and decoys.
- **Fuel Engine** (`power.fuel-engine`) ⭐ — cheap, high power density; built from cylinders/injectors.
- **Steam Engine** (`power.steam-engine`) ⭐ — boilers, turbines, pistons; high efficiency or high output.
- **RTG** (`power.rtg`) — radioisotope generators feeding Engine Power.
- **Battery** (`power.battery`) — energy storage feeding Engine Power.

## Defence Systems
- **Shield** (`defence.shield`) ⭐ — ring shields (armor-class boost) and planar/projector shields (deflection).
- **LAMS** (`defence.lams`) — Laser Anti-Munition System.
- **Jammer / Decoy** (`defence.softkill`) — chaff, flares, heat decoys, radar/sonar simulators, signal jammers, and smoke dispensers.
- **Armor** (`defence.armor`) *(module)* — armor plating (also the buoyancy subsystem).

## Detection / Sensors
- **Sensor** (`detect.sensor`) — all detection: radar, sonar, cameras/IR, laser rangefinders, snoopers, and munition warners. Split in-game into **Detectors** (wide FOV, autonomous) and **Trackers** (narrow FOV, precise).

## AI / Control
- **AI Mainframe** (`ai.mainframe`) — AI mainframes and behavior cards (naval, air, missile-guidance, etc.).
- **Breadboard / ACB / PID** (`ai.breadboard`) — logic and control systems.
- **Storage** (`ai.storage`) — material, fuel, ammo, and energy containers.

## Sources
- [Vehicle](https://fromthedepths.wiki.gg/wiki/Vehicle)
- [Weapons](https://fromthedepths.fandom.com/wiki/Weapons)
- [Defence Systems](https://fromthedepths.wiki.gg/wiki/Defence_Systems)
- [Detection](https://fromthedepths.wiki.gg/wiki/Detection)
- [Engine Power](https://fromthedepths.wiki.gg/wiki/Engine_Power)
