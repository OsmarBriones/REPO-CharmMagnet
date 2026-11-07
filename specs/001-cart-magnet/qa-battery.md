# QA battery: Cart Magnet (CharmMagnet)

Deploy: Pockets, CharmCore and CharmMagnet are in the r2modman `Debug` profile (compare hashes with the builds). Restart the game first.

## Quick start with debug mode
Set `DebugMode = true` in the `Debug` section of `BepInEx/config/com.osmar.CharmMagnet.cfg` (off by default). When a level starts, a Magnet is placed on the floor about 2.5 m in front of you; the log then also shows the detailed lines. Any level works. To get another one at any moment, set `SpawnNow = true` in the same section while a level is running: the valuable is placed near you and the switch turns itself back off (the file is re-read about once a second).

## Single player
- [ ] Log shows `Charm registered: Magnet - Cart Magnet` and `CharmMagnet 1.0.0 loaded!`.
- [ ] Store the valuable in a slot: the slot shows its picture and value.
- [ ] Spawn some Tiny/Small valuables around (or use a level). Without a cart in hand: nothing moves.
- [ ] Hold a cart (grab it) with the Magnet pocketed: about once per second the nearest loose Tiny/Small valuable within 6 m jumps into the cart and stays there, not falling out or overlapping.
- [ ] Each valuable moved lowers the Magnet's value by about 4% (log with DebugMode shows each pull and wear step).
- [ ] Release the cart: pulling stops. Magnet out of the pocket: pulling stops.
- [ ] Not pulled: a valuable in your hand, one in a pocket, one already in a cart, Medium or bigger ones, ones beyond 6 m, ones inside the extraction room.
- [ ] Push the cart into the extraction room: the pulled valuables count in the haul.
- [ ] Take the valuable out: the power stops.
- [ ] Use it until the valuable breaks: the slot empties, the power stops, no errors in the log.

## Settings
- [ ] `WearPercent = 0`: no wear. `Radius`, `IntervalSeconds` and `MaxPerCarrier` (try 3) change the behaviour as described.
- [ ] Charm `Enabled = false`: no effect and no wear. CharmCore `Enabled = false`: nothing works.

## Multiplayer (host and client)
- [ ] Client carries the Magnet and holds a cart (host in the same lobby): valuables jump into the client's cart (this checks that the host sees the client's cart grab); the Magnet value drops on both machines.
- [ ] Pulled valuables stay in place on the client's screen and are not snapped back by their owner's physics.
- [ ] Host carries a Magnet while a client holds the cart: nothing happens (the carrier must hold the cart).
- [ ] Host settings override the client's.
- [ ] Client leaves and rejoins: no stuck veil.

Log patterns: `Charm registered`, `threw`, `Exception`.
