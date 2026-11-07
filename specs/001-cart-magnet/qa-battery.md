# QA battery: Cart Magnet (CharmMagnet)

Deploy: Pockets, CharmCore and CharmMagnet are in the r2modman `Debug` profile (compare hashes with the builds). Restart the game first.

## Quick start with debug mode
Set `DebugMode = true` in the `Debug` section of `BepInEx/config/com.osmar.CharmMagnet.cfg` (off by default). When a level starts, a Magnet is placed on the floor about 2.5 m in front of you; the log then also shows the detailed lines. Any level works.

## Single player
- [ ] Log shows `Charm registered: Magnet - Cart Magnet` and `CharmMagnet 1.0.0 loaded!`.
- [ ] Store the valuable in a slot: the slot shows its picture and value.
- [ ] TODO: trigger the power and check the effect, the wear on the valuable and the cooldown veil on the slot.
- [ ] Take the valuable out: the power stops.
- [ ] Use it until the valuable breaks: the slot empties, the power stops, no errors in the log.

## Settings
- [ ] `CooldownSeconds = 0`: no veil, no pause. `WearPercent = 0`: no wear.
- [ ] Charm `Enabled = false`: no effect and no wear. CharmCore `Enabled = false`: nothing works.

## Multiplayer (host and client)
- [ ] The effect works on the client, the value drops on both machines.
- [ ] Host settings override the client's.
- [ ] Client leaves and rejoins: no stuck veil.

Log patterns: `Charm registered`, `threw`, `Exception`.
