# Changelog

## [Unreleased]

### Added
- Cart Magnet: while you hold a cart, loose Tiny and Small valuables within `Radius` metres jump into it, `MaxPerCarrier` every `IntervalSeconds`; each valuable moved wears the Magnet by `WearPercent`.
- Settings `Radius`, `IntervalSeconds` and `MaxPerCarrier`.
- `Debug.DebugMode` (off by default): puts the Magnet on the floor at the start of each level and writes detailed logs.

### Changed
- Runtime hygiene pass: carts are cached instead of searched on every pull, per-level memory is cleared when a level ends, and the per-move collider scan no longer allocates.
