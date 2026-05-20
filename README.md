# plateup_god_mod

A single-DLL developer / cheat mod for [PlateUp!](https://store.steampowered.com/app/1599600/PlateUp/). Uses the game's native mod loader (`KitchenMods`) — no BepInEx, no Harmony, no KitchenLib required.

## Features

- **Infinite patience** — customers never get angry while it's on. Toggle live from the menu.
- **Speed control** — 0.25x / 0.5x / 1x / 2x / 4x / 8x global simulation speed.
- **Item spawner** — drop any food item (final dish, ingredient, or in-progress state) directly into the player's hand. Three tabs: *Dishes* (orderable items from any unlocked menu), *All items*, and *Appliances*. Live search box.
- **Appliance spawner** — place any appliance (stove, oven, coffee machine, table, decor, …) one tile in front of the player.
- **Quick hotkeys** — `Ctrl+1`/`Ctrl+2`/`Ctrl+3`/`Ctrl+4` to instant-spawn Coffee / Pizza / Dumpling / Hot Dog.

## Controls

| Key | Action |
| --- | --- |
| `` ` `` (backtick / tilde) | Toggle the in-game menu overlay |
| `Ctrl+1` | Spawn Coffee in hand |
| `Ctrl+2` | Spawn Pizza |
| `Ctrl+3` | Spawn Dumpling |
| `Ctrl+4` | Spawn Hot Dog |

The menu lets you toggle infinite patience, change game speed, and click a button to spawn any item or appliance by name.

## Install (binary)

1. Download `InfinitePatience.dll` from [Releases](https://github.com/HezhaKh/plateup_god_mod/releases) (or build from source — see below).
2. Find your PlateUp install folder — the one containing `PlateUp.exe`. Typical locations:
   - `C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\`
   - `D:\SteamLibrary\steamapps\common\PlateUp\PlateUp\`
3. Create a folder `Mods\InfinitePatience\` inside that install folder.
4. Drop `InfinitePatience.dll` into `Mods\InfinitePatience\`.
5. Launch the game. The mod loads automatically.

Final layout:

```
<PlateUp>\
  PlateUp.exe
  PlateUp_Data\
  Mods\
    InfinitePatience\
      InfinitePatience.dll
```

To uninstall, delete the `Mods\InfinitePatience\` folder.

## Build from source

Requires the [.NET SDK 8](https://dotnet.microsoft.com/download) (or any newer SDK that supports `net472` target framework).

```powershell
git clone https://github.com/HezhaKh/plateup_god_mod.git
cd plateup_god_mod
dotnet build -c Release
```

The project tries common Steam locations automatically. If it can't find PlateUp, point it explicitly:

```powershell
# one-off:
dotnet build -c Release -p:PlateUpDir="D:\Games\PlateUp\PlateUp"

# or set an environment variable (persists per shell):
$env:PLATEUP_DIR = "D:\Games\PlateUp\PlateUp"
dotnet build -c Release
```

`PlateUpDir` is the folder containing `PlateUp.exe`. A successful build copies the DLL into `<PlateUpDir>\Mods\InfinitePatience\` automatically.

## How it works

- **Patience**: PlateUp ships with a built-in debug singleton `Kitchen.SCheatNoPatienceDecrease`. The game's own `UpdateQueuePatience` and `UpdateCustomerImpatience` systems already short-circuit when that singleton exists. The mod creates / destroys that singleton based on the menu toggle — that's the whole patience implementation.
- **Speed**: The mod reads & writes the `float GameSpeed` field of the `Kitchen.SGameTime` singleton. PlateUp's `UpdateTime` system multiplies the per-frame `DeltaTime` by `GameSpeed`, which scales the whole simulation.
- **Item spawning**: Creates an entity with `Kitchen.CCreateItem { ID, Holder = player }`. The game's `CreateNewItems` system materializes it into the player's hand.
- **Appliance spawning**: Creates an entity with `Kitchen.CCreateAppliance { ID } + Kitchen.CPosition { player.ForwardPosition rounded }`. The game's `CreateNewAppliances` system materializes the appliance.
- **Menu**: A `MonoBehaviour` hosted by `IModInitializer.PostInject()`, rendered with Unity's IMGUI. Reads all `KitchenData.Item`, `Appliance`, and `Dish` assets from `KitchenData.GameData.Main.Objects`.

No game files are patched. Removing the DLL fully reverts behavior.

## Caveats

- This is a dev / cheat tool — it does not pretend to be balanced. Don't expect achievements to unlock while it's installed.
- Appliances spawn at the tile directly in front of the player. If that tile is occupied (wall, counter, another appliance), the game will refuse the placement silently.
- The IMGUI overlay is intentionally barebones — it does not match PlateUp's UI style. Functional, not pretty.
- Customers on fire (from kitchen fires) still lose patience — that path bypasses the cheat singleton. Avoid fires.
