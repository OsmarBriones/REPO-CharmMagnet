# Charm Magnet

Cart Magnet: while you hold a cart, loose small valuables nearby jump into it, one after another.

> **Everyone in the lobby must install this mod, together with Pockets and CharmCore.** The power changes what your own character can do and its state shows on your inventory slot, so friends without the mods would see an empty slot and be unable to use the pocket.

## How to Use

1. Find the **Magnet** valuable and store it in a free inventory slot with [Pockets](https://thunderstore.io/c/repo/p/OsmarBriones/Pockets/) (grab it, press the slot key).
2. Grab a cart and hold it. Every second, one loose Tiny or Small valuable within 6 metres of the cart jumps into it. Pieces you or a friend are holding, pieces already in a cart, pieces inside the extraction room and pieces already committed to extraction are left alone.
3. Take the valuable out of the pocket and the power stops.

## Features

- **Cart Magnet:** hold a cart and nearby loose Tiny and Small valuables fly into it, nearest first. The haul only counts once you push the cart into the extraction room, as usual.
- **Works for every carrier:** each player who carries a Magnet and holds a cart pulls for their own cart.
- **It costs the valuable:** each valuable pulled wears away a share of the Magnet's value, and a worn-out valuable breaks.
- **Everything is configurable:** wear, distance, speed and amount to this power. The host's choices apply to everyone.

## Requirements
- [BepInEx Pack for R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
- [Pockets](https://thunderstore.io/c/repo/p/OsmarBriones/Pockets/)
- [CharmCore](https://thunderstore.io/c/repo/p/OsmarBriones/CharmCore/)

## Installation
1. Install through r2modman / Thunderstore Mod Manager (Pockets and CharmCore come with it), or place `CharmMagnet.dll` next to `Pockets.dll` and `CharmCore.dll` in `BepInEx/plugins`. **All players need it.**
2. Launch the game once; the configuration file is created inside `BepInEx/config`.

## Configuration
Settings are in `com.osmar.CharmMagnet.cfg` inside `BepInEx/config`, section `Charm.Magnet`. The host's choices apply to everyone.

- `Enabled` (default `true`): turn this charm on or off.
- `WearPercent` (default `4`): share of the Magnet's original value lost for each valuable pulled.
- `Radius` (default `6`, 1 to 20): metres from the cart within which valuables are pulled.
- `IntervalSeconds` (default `1`, 0.25 to 10): seconds between pulls.
- `MaxPerCarrier` (default `1`, 1 to 5): valuables pulled at each pull, per player.

Section `Debug`: `DebugMode` (default `false`) places a Magnet on the floor in front of you when a level starts (host or singleplayer) and writes extra log lines. For testing. `SpawnNow` (default `false`, needs `DebugMode`): set it to `true` while a level is running to place one right away; it switches itself back off.

## Issues & Bug Reports
The official way to report issues, suggest improvements, or submit feedback is by opening an issue on the official GitHub repository:
👉 [GitHub Issues](https://github.com/OsmarBriones/REPO-CharmMagnet/issues)

## Credits
Developed by **Osmar Briones**
