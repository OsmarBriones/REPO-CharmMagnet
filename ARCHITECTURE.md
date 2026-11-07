# CharmMagnet Architecture

A Charm mod (layer 3 of the PocketCharms family): one valuable, one power. It depends on **Pockets** (storage) and **CharmCore** (registry, wear, host settings, debug mode).

## The power: Cart Magnet

While a player carries the Magnet (`Valuable Desert Magnet`) in a pocket AND is holding a cart, loose Tiny and Small valuables within `Radius` metres of the cart are teleported into the cart, `MaxPerCarrier` every `IntervalSeconds`. Each valuable moved wears the carrier's Magnet by `WearPercent` (default 4%).

This is a HOST effect. Teleporting a world valuable only works on the master (`PhysGrabObject.Teleport` does nothing for non-player objects on a client; `SetPositionRPC` is master only), so nothing runs on the owner's machine.

- `MagnetCharm : Charm` registers the prefab and the three settings below. `WearsPerSecond` is false: wear is per valuable moved.
- `CartMagnet` (a `MonoBehaviour` on the plugin object). In `Update`, only when `SemiFunc.IsMasterClientOrSingleplayer()`, once per `IntervalSeconds` it calls `CharmsApi.ForEachCarrier(charm, (avatar, magnet) => ...)`:
  1. Find the cart the carrier holds: among all `PhysGrabCart` (found once per tick), the one whose `PhysGrabObject.playerGrabbing` contains `avatar.physGrabber` and whose `impactDetector.isCart` is true.
  2. Build a `CandidateInfo` for every entry of `ValuableDirector.instance.valuableList` and keep those `MagnetRules.IsEligible` accepts, nearest first.
  3. For the first `MaxPerCarrier`: compute a spot in the cart volume, zero `rb.linearVelocity`/`angularVelocity`, call `PhysGrabObject.Teleport`, then `charm.WearValuable(magnet, charm.WearPercent)`.
- `Domain/MagnetRules.cs` (game-free, unit tested): `IsEligible` (the candidate filter), `PlaceInBox` (position inside the box), `NextLevel` (stack layer), `IsInsideBox`.

### Candidate filter (all must hold)
Tiny or Small `volumeType`; distance from the cart volume centre <= `Radius`; not held (`grabbed` or `playerGrabbing.Count > 0`); not pocketed (`PocketsApi.IsPocketed`); not in a cart (`impactDetector.inCart`, `cart.itemsInCart`, or geometrically inside this cart's volume); not `committedToExtraction`; not in an extraction room (`roomVolumeCheck.CurrentRooms` with `Extraction`); game object active and not at `AssetManager.physDisabledPosition`; not `dead`; not the carrier's Magnet; not moved by the magnet in the last 3 s.

### Where the piece lands
`PhysGrabCart.inCart` is the volume transform; its `localScale` is the box size (the game's own `Physics.OverlapBox(inCart.position, inCart.localScale / 2f, inCart.rotation)` in `ObjectsInCart`). The centre of the piece is placed at a random spot of the floor plane, kept inside the box minus the piece's extents (taken from its non-trigger colliders), with its bottom 0.1 m above the box floor. Pieces are spread over 4 stack layers (by how many the cart already holds plus how many were just pulled) so they do not overlap; the layer is clamped to the box top. The offset is rotated by `inCart.rotation` (not `TransformPoint`, which would multiply by the box scale). The collider centre offset from the pivot is subtracted so the visible piece, not its pivot, lands on the spot.

### Extraction
Pulled valuables only count for the haul when the cart is pushed into the extraction room (normal game behaviour: the cart's content is evaluated there). Valuables already inside the extraction room or committed to extraction are never pulled.

## Settings (AddSetting, host-synchronised and clamped)

| Name | Type, unit | Range | Default | Notes |
|------|-----------|-------|---------|-------|
| `Radius` | float, metres | 1 to 20 | 6 | negative or zero make no sense; 20 covers most of a room |
| `IntervalSeconds` | float, seconds | 0.25 to 10 | 1 | 0 would pull every frame, so the minimum is 0.25 |
| `MaxPerCarrier` | float read as a whole number (rounded) | 1 to 5 | 1 | per carrier and per pull |

Plus the provided `Enabled`, `WearPercent` (per valuable moved) and `Debug.DebugMode`. `defaultCooldownSeconds` is null: there is no cooldown; the interval plays that role.

## Game members used

Checked by `CharmMagnet.Tests/GameContractTests.cs`: `ValuableObject.volumeType/committedToExtraction/roomVolumeCheck/physGrabObject`, `ValuableDirector.instance/valuableList`, `PhysGrabCart.inCart/itemsInCart`, `PhysGrabObject.playerGrabbing/grabbed/dead/rb/impactDetector/Teleport`, `PhysGrabObjectImpactDetector.isCart/inCart`, `PlayerAvatar.physGrabber`, `RoomVolumeCheck.CurrentRooms`, `RoomVolume.Extraction`, `AssetManager.physDisabledPosition`, `SemiFunc.IsMasterClientOrSingleplayer`. From mods: `PocketsApi.IsPocketed`, `CharmsApi.ForEachCarrier`, `Charm.WearValuable`.

## Decisions

- **Held-cart detection uses `playerGrabbing` on the cart**, not `PhysGrabber.grabbedPhysGrabObject`. The latter is set in `StartGrabbingPhysObject` on the grabbing player's machine; the host is not guaranteed to have it for remote players. `playerGrabbing` is filled on the host through `GrabPlayerAddRPC` (sent by the master to everyone) and directly when alone.
- Pulled pieces are ignored for 3 s after a move because the cart refreshes `itemsInCart`/`inCart` on a timer.
- Nearest-first order, so the piece closest to the cart goes first.
- No cooldown veil: the Magnet has no cooldown, wear is per valuable.
- Carts are found with `Object.FindObjectsByType` once per pull tick only when some carrier exists (cheap at 1 Hz).
- The RepoKit submodule was not synchronised for this task (it is another repository, off limits).

## Known limits and NOT verified (nothing was run in the game)

- The host's view of a remote player's cart grab (`playerGrabbing`) is read from the decompiled code, not observed.
- `Teleport` in multiplayer goes through `photonTransformView.Teleport`; whether a Tiny/Small valuable owned by a client (`clientNonKinematic`) keeps the new position without being overwritten by the owner is unverified. Velocity is zeroed on the host's rigidbody only.
- The landing spot assumes the cart volume is a box of size `inCart.localScale` in world units (as the game's overlap box does); a cart whose parents are scaled would differ.
- Small carts (`isSmallCart`) are treated like any cart.
- Valuables moved but later hit by the cart can still break normally.
- Looked up by exact prefab name; a game update that renames the prefab needs a mod update.

## Debug mode

Provided by CharmCore: `Debug.DebugMode` (off by default) places the Magnet on the floor at the start of each level and enables `Charm.LogDebug`; each pull logs the valuable, distance and carrier.
