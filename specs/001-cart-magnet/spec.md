## User Scenarios

### User Story 1 - Cart Magnet (Priority: P1)
A player stores the Magnet in a pocket and gets this power while it stays there: while you hold a cart, loose Tiny and Small valuables within 6 m (configurable) jump into it, one per second (configurable).
**Acceptance**: with the valuable pocketed the power works; with it out of the pocket it does not; each valuable moved wears the Magnet by 4%.

## Requirements
- FR-001: Registers a charm with CharmCore for the prefab Valuable Desert Magnet.
- FR-002: Settings through CharmCore: Enabled, WearPercent, Radius, IntervalSeconds, MaxPerCarrier, DebugMode; host-authoritative except DebugMode.
- FR-003: All players install it (Category 2); depends on Pockets and CharmCore.