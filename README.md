# Extended Fleet Commands — a Star Valor mod

Adds an **Orders** section to each fleet ship's behaviour panel (the panel with its role, tactic
and emergency-warp settings). The orders act on **your current target**:

- **Tow**: the ship locks its Tractor Beam onto the object and tows it while it keeps formation
  with you. Use this to haul wrecks and containers to a station.
- **Load** / **Load nearest**: the ship flies to the loot and loads it into its own cargo hold.
- **Defend**: the ship leaves your formation to guard a friendly ship or station. It attacks
  whatever that ship or station is fighting, anything attacking it, and hostiles near it. A ship
  with the Repair role repairs it instead.
- **Escort**: the ship follows another ship instead of you, without starting fights. With
  **Collect loot** on, it also picks up loot that drops near that ship, for example a freighter
  shadowing a mining ship and collecting its ore.
- **Cancel**: the ship drops its order and goes back to formation.

It also adds one behaviour setting:

- **Only use mining gear when mining**: a ship with the **Mining** role uses only its mining
  lasers and mining drones on asteroids, and keeps its other weapons and drones quiet. A ship with
  no mining gear doesn't mine at all, as if it found no asteroid. On a ship of any role, a turret
  that has a mining laser on it fires only the laser at asteroids.

## Using it

1. Target the wreck, container, loot, ship or station you want the order to apply to.
2. Open the fleet panel and open the fleet ship's behaviour panel.
3. The **Orders** section at the bottom names your target and shows the ship's current order.
   Press the order you want.

A button is only available when the order fits your target:

| Target | Orders available |
|---|---|
| Drifting wreck or container | Tow, Load (if it fits in the hold) |
| Friendly or neutral ship, or another fleet ship | Defend, Escort |
| Friendly or neutral station | Defend |
| Anything else, or nothing | Load nearest (the nearest loot the ship can carry) |

**Cancel**, next to the ship's current order, is available while the ship has an order. The buttons are greyed out when the ship is
docked, in your hangar or in another sector.

The ship reports in the message log (left side of the screen) when it takes an order, locks its
tractor beam, loads something, runs out of cargo space, or the order ends.

### Rules the ships follow

- **Tow** needs a **Tractor Beam** fitted to the fleet ship. It has the same range and pulling force as
  when you use one yourself. The ship flies close enough to lock on, then follows you as usual. If
  the tow line breaks (too far, or out of energy), it goes back and locks on again. Towing ships
  don't start fights.
- **Load** follows the game's own pickup rules. A wreck has to be a smaller ship class than the fleet
  ship, and it has to fit in the hold. Whatever is loaded stays in the fleet ship's cargo, the same
  as loot collected with the **Collect loot** behaviour. Loading ships don't start fights on the way.
- **Defend** keeps the ship next to the defended ship or station, wherever it goes. It won't chase
  attackers further than twice `DefendRadius` from it. The order ends if the defended ship or station
  is destroyed, leaves, or turns hostile to you.
- **Escort** keeps the ship trailing behind and to one side of the escorted ship. The escort doesn't
  start fights, even if the escorted ship is attacked; use Defend for that. If the escort's
  **Collect loot** behaviour is on, it picks up loot within `EscortCollectRadius` of the escorted
  ship and then falls back in behind it. When its hold fills up, it says so and keeps escorting
  without collecting. Loot its loot filter rejects is left alone. The order ends if the escorted ship
  is destroyed, leaves, or turns hostile to you.
- Any order ends when you give the fleet (or that ship) a direct attack order, when the ship docks
  at a station or your carrier, or when it makes an emergency warp.

### Only use mining gear when mining

The toggle sits just above the Orders section. For ships with the **Mining** role, while they are
working an asteroid:

- Weapons that do bonus damage to asteroids (mining lasers), fixed or turret-mounted, fire as
  usual. Every other weapon holds fire.
- Mining drones launch as usual. Combat drones stay in the hangar, and any already out are
  recalled.
- If the ship has no mining weapons or mining drones, it doesn't look for asteroids and lets go of
  the one it was mining. It stays in formation like a miner with no asteroid in range. Switching the
  toggle on for a ship like that puts a note in the message log.
- When the ship fights (for example, it is attacked), it uses all its weapons and drones as usual.

A turret with a mining laser on it shoots at nearby asteroids by itself whenever it has nothing
else to shoot at, whatever the ship's role, and normally fires every weapon on that turret at them.
With the toggle on, only the mining laser fires at asteroids, for ships of any role.

Unlike orders, this setting is saved with the game.

### Limitations

- Orders aren't saved. Warping to another sector, docking, or loading a game clears them.
- Objects your fleet is towing don't come with you when you warp. Only your own tractor beam counts
  toward warp towage.

## Installation

The mod needs **BepInEx 5**, the standard mod loader for Star Valor. If you already use other
Star Valor BepInEx mods, skip to step 2.

### 1. Install BepInEx 5

1. Download **`BepInEx_win_x64_5.4.23.2.zip`** from the
   [BepInEx 5.4.23.2 release](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2).
   Use the **Windows x64** build of **BepInEx 5**, even on Linux. BepInEx 6 will not work.
2. Open the game folder. In Steam: right-click **Star Valor** → **Manage** → **Browse local files**.
3. Extract the zip into that folder. `winhttp.dll`, `doorstop_config.ini` and the `BepInEx` folder
   must end up **next to `Star Valor.exe`**, not in a subfolder.
4. **Linux / Steam Deck (Proton) only:** in Steam, right-click **Star Valor** → **Properties** →
   **General** → **Launch Options**, and enter:
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```
   Without this, Proton ignores BepInEx and no mods load.
5. Start the game once, then quit. BepInEx creates `BepInEx/plugins`, `BepInEx/config` and
   `BepInEx/LogOutput.log`.

### 2. Install Extended Fleet Commands

1. Download **`ExtendedFleetCommands-<version>.zip`** from the
   [Releases page](https://github.com/cydergoth/extended_fleet_commands/releases/latest).
2. Extract it into the game folder (the same folder as `Star Valor.exe`). This creates
   `BepInEx/plugins/ExtendedFleetCommands/`, which holds `ExtendedFleetCommands.dll` and a
   `Language` folder.
3. Start the game, target something, then open a fleet ship's behaviour panel.

### Checking it worked

`BepInEx/LogOutput.log` should contain:

```
[Info   :Extended Fleet Commands] Extended Fleet Commands 1.1.0 loaded
```

If the log file doesn't exist, BepInEx isn't running. Check that the files are next to
`Star Valor.exe`, and on Linux check the launch option. If the log says the mod is not compatible
with this version of the game, a game update changed something the mod relies on. Please
[open an issue](https://github.com/cydergoth/extended_fleet_commands/issues) and attach the log.

### Updating

Download the new release and extract it over the old one, replacing the files in
`BepInEx/plugins/ExtendedFleetCommands/`. If you edited a language file, back it up first.

### Uninstalling

- To remove only this mod, delete `BepInEx/plugins/ExtendedFleetCommands/`.
- To remove BepInEx too, delete `BepInEx/`, `winhttp.dll`, `doorstop_config.ini` and
  `changelog.txt` from the game folder, and clear the launch option.

You can remove the mod at any time. The only thing it saves is the **Only use mining gear when
mining** setting, which is kept in a spare field of the ship's behaviour settings that the game
itself never reads, so removing the mod leaves your saves working as before.

## Configuration

`BepInEx/config/starvalor.extendedfleetcommands.cfg` is created the first time the game runs with
the mod. Edit it while the game is closed.

| Setting | Default | Meaning |
|---|---|---|
| `DefendRadius` | `300` | How far from the defended ship or station a defender engages hostiles |
| `LoadSearchRange` | `600` | How far from the fleet ship **Load nearest** looks for loot |
| `EscortCollectRadius` | `200` | How far from the escorted ship an escort with **Collect loot** on picks up loot |

## Translations

The mod reads the game's language setting and loads its own text from
`BepInEx/plugins/ExtendedFleetCommands/Language/<language>_main.txt`. These files use the same
format and names as the game's own `Language` folder (`german_main.txt`, `chinese_main.txt`, ...):

```
001 Tow|
020 <par1>: tractor beam locked on <par2>.|
```

- Each entry is a three-digit number, a space, the text, and a `|` at the end.
- Lines that start with `#` are comments.
- `<par1>` and `<par2>` are replaced with values, such as a ship or object name.
- Save the file as UTF-8.

The mod looks entries up by their number. If a file leaves an entry out, or the language has no
file, the mod uses the English text built into the DLL.

The non-English files were drafted with AI and may read awkwardly. To fix a translation or add a
language, copy `english_main.txt`, rename it to the game's name for that language, and translate
the text after each number. Please send corrections as a pull request or an issue.

## Compatibility

- Built against Star Valor Steam build 24937123 (Unity 2019.4.41f1) and BepInEx 5.4.23.2.
- Works alongside [Production Summary](https://github.com/cydergoth/production_summary).
- Achievements are unaffected. The game has no mod detection, and this mod doesn't touch perks,
  stats or Steam.
- The mod changes how fleet ships behave only while they have one of its orders, or while a Mining
  ship with **Only use mining gear when mining** on is mining, or while a turret on a ship with that
  setting on aims at an asteroid. Otherwise the game's own AI runs unchanged.
- If a game update renames something the mod relies on, the mod disables itself at startup and
  writes the reason to `BepInEx/LogOutput.log`. The rest of the game keeps working.

## Building from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer and a Star Valor
install, because the build references the game's own DLLs. Those DLLs are not included in this
repository.

```bash
./install.sh     # build and copy the DLL into the game's BepInEx/plugins folder
./package.sh     # build and create the release zip in dist/
# or build only:
dotnet build -c Release -p:GameDir="/path/to/Star Valor"
```

`GAME_DIR` (for the scripts) and `-p:GameDir` (for `dotnet build`) default to the standard Linux
Steam location. The project file also checks the default Windows Steam path.

### How it works

**Orders section.** A postfix on `FleetBehaviorControl.ShowData`, which runs whenever the behaviour
panel opens or switches ship, adds the section the first time and refreshes it. The panel's controls
are anchored to its top, so the mod makes the panel 126 px taller and puts the mining toggle and
the Orders section in the new space above the Close button. Its labels, buttons and toggle are
copies of the panel's own, and it refreshes four times a second while open so it follows your
target.

**Orders.** An order is a small `FleetOrder` component on the fleet ship's GameObject, so it goes
away with the ship. These patches read it:

- `AIMercenary.SetActions` (the AI's decision step, about once a second): runs the order's rules.
  Tow, Load and Escort let the game's step run as well. Defend replaces it, because the game's version pulls
  the ship back to the player and only reacts to threats against the player.
- `AIMercenary.SetNewDestination`: points the ship at the object to tow or load, at a guard
  position next to the defended ship or station, or at a trailing position behind the escorted
  ship (or at loot near it). Once the tow line is attached, the game's
  follow-the-player movement takes over.
- `AIMercenary.SetNewTarget`, `SetRepairTarget` and `SearchForAsteroids`: blocked during Tow, Load
  and Escort so the ship doesn't wander off to fight, repair or mine.
- `AE_TractorBeam.ShouldBeActivated`: while towing, the AI's combat logic leaves the tractor beam
  alone.

**Only use mining gear when mining.** The setting is a bit in `FleetMemberBehavior.repairTactic`,
which the game saves with each fleet ship but never reads. While a ship with the setting mines:

- `AIMercenary.CanFireWeaponType` and `WeaponTurret.CanFireWeaponType` allow only weapons with
  `DamageType.AsteroidBonus`.
- Around `AIControl.Update`, which launches every drone bay at the ship's target, the ship's list of
  attack drone bays is narrowed to its mining drone bays, and combat drones that are out are
  recalled.
- `AIMercenary.SearchForAsteroids` is blocked if the ship has no mining weapons or drones.

A `WeaponTurret` with a mining weapon looks for asteroids itself (`SearchForMiningTarget`) whatever
the ship's role. So for any ship with the setting on, `WeaponTurret.CanFireWeaponType` also stops a
turret from firing its non-mining weapons at an asteroid.

Towing uses the ship's own `AE_TractorBeam`, so tow range, force and energy use match the player's.
Loading calls `CargoSystem.CollectItem`, the same method fleet ships use to collect loot, so cargo
space, loot filters and wreck-size limits all apply.

## AI disclosure

This mod's code and documentation were written with the AI coding assistant
[Claude Code](https://claude.com/claude-code), directed and reviewed by the author. Commits made
with it carry a `Co-Authored-By: Claude` trailer. If you share the mod on sites that label AI
content, such as Nexus Mods, tag it as AI-generated code.

Star Valor is made by Rafael Burgos. This is an unofficial fan mod, and the developer does not
endorse or support it.

## License

[MIT](LICENSE) © 2026 cydergoth. The license covers this mod's source code only. Star Valor and its
assets belong to their developer, and BepInEx has its own license.
