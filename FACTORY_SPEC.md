# Factory Simulation Specification

Status: **Approved for implementation on 2026-09-23.**

This document captures the authoritative requirements for the MonoBehaviour reference factory. It is derived from the supplied `Unity_Factory_MonoBehaviour_Prompt.txt`, treated as specification input rather than an instruction to implement during the current review phase.

## 1. Goal and non-goals

Create a complete, playable 3D factory diorama whose logical results can later serve as the reference for a DOTS port. The reference implementation uses MonoBehaviours and ordinary C# state only, while exposing implementation-neutral scenarios, snapshots, and events suitable for exact per-tick comparison.

The scene should evoke Factorio/Satisfactory with a raised orthographic camera, primitive models, visible production, moving items, readable status, and controls. It does not include a player character, building/construction flow, inventories, crafting menus, persistence, complex physics, splitters, mergers, or routing.

Authoritative item movement and transfer must not depend on Rigidbody collisions, triggers, animations, presentation transforms, or proximity.

## 2. Project baseline

- Repository root: this file's directory.
- Unity project: `FabriccaBellissima/`.
- Unity: `6000.3.10f1`.
- URP: `17.3.0`.
- Unity Test Framework: `1.6.0`.
- Starting scene: `FabriccaBellissima/Assets/Scenes/MainGameScene.unity`.
- Use built-in features and primitive geometry; add no paid assets, external downloads, or unnecessary packages.
- Prefix non-public instance fields with `_`.

## 3. Required topology

The saved demo contains these explicit one-way port connections:

```text
IronOre extractor -> ore conveyor -------> furnace ore input
Coal extractor ----> coal conveyor ------> furnace fuel input
                                           furnace IronBar output
                                                     |
                                                     v
PaintCan extractor -> paint conveyor ---> coloring station can input
                         bar conveyor ---> coloring station bar input
                                           coloring station PaintedIronBar output
                                                     |
                                                     v
                                              output conveyor -> sink
```

The `bar conveyor` starts at the furnace output. Each conveyor has exactly one upstream and one downstream connection. All node positions and port connections are Inspector-editable and serialized. The implementation validates duplicate IDs, missing connections, unsupported cycles/topology, incompatible ports, and invalid numeric configuration with clear errors.

### Stable identity

Every node and port has an explicit stable ID defined by the scenario/serialized setup. IDs must be unique and must not derive from Unity instance IDs, transform order, object names discovered at runtime, or dictionary iteration. Transfer ordering uses `(sourceNodeId, sourcePortId)` ascending ordinal order. Node-local update and simultaneous creation use stable `nodeId` ascending order.

## 4. Resources and items

Resource types:

- `IronOre`
- `Coal`
- `PaintCan`
- `IronBar`
- `PaintedIronBar`

Each transported item is an individual logical record with a globally unique, monotonically allocated simulation ID. A transformation preserves the input item's ID: ore becomes an iron bar with the same ID, and that bar becomes a painted bar with the same ID. Consumed coal and opened paint cans retain their own IDs in events/accounting but do not become output items.

Paint cans carry a positive integer charge count and a paint color ID. Painted bars retain the applied color ID. A run has exactly one configured compatible paint color, default `Blue`; mixed-color painting is outside scope. Wrong resources or colors are rejected without mutation or consumption.

## 5. Time, integer conversion, and control

### Fixed tick

- Default tick duration: `0.05` seconds.
- Inspector durations are seconds and are converted once during initialization to ticks with `ceil(seconds / tickDuration)`.
- Every configured positive processing duration produces at least one tick.
- Converted tick counts are authoritative for the run and do not change until reinitialization/reset with a new scenario.
- Simulation time is `executedTickCount * tickDuration`; it never depends on FPS, wall clock, machine speed, or render timing.

### Conveyor units

- Use `1,000` integer distance units per Unity world unit.
- Convert belt length once with `ceil(lengthWorldUnits * 1,000)`.
- Convert movement once with `ceil(speedWorldUnitsPerSecond * tickDuration * 1,000)`, with a minimum of one distance unit per tick for a positive speed.
- This explicit upward rounding is part of the scenario semantics. Snapshots include the converted integer values so another implementation need not reproduce floating-point conversion.
- Render positions may be interpolated from integer progress, but rendered values never affect logic.

### Coordinator and controls

One MonoBehaviour simulation coordinator exposes `StepOneTick`. It owns the accumulator used only to decide how many whole ticks to request during Play. It supports:

- Play and Pause.
- Reset to the exact initialized scenario state.
- Single Tick while paused.
- Simulation speed controlling tick scheduling only; it never changes configured belt speed or machine durations.
- Optional batched advancement that is exactly repeated `StepOneTick` calls and never changes results.

The coordinator never drops/skips owed ticks as a catch-up shortcut. If a per-frame execution budget is used to keep the UI responsive, unexecuted ticks remain queued. Stations and belts never independently advance authoritative state.

## 6. Authoritative tick contract

For tick `T`, all nodes have access to state committed at the end of `T-1`.

### Phase 1: stable local update

Visit nodes in stable node-ID order. Advance extractor work, furnace work/fuel, coloring work, and conveyor movement using only state that existed at tick start. A furnace may ignite already-waiting coal and a coloring station may open an already-waiting can here.

When multiple extractors complete on the same tick, this stable visitation order determines global item-ID allocation. Local events follow the same deterministic order.

### Phase 2: gather and resolve transfers

After every local update completes:

1. Gather at most one proposal from each eligible output port, based on the post-local-update state. A proposed item remains owned by its source.
2. Sort proposals by `(sourceNodeId, sourcePortId)`.
3. Visit every proposal once. Check destination acceptance against the current committed phase-2 state, so space freed by an earlier successful commit may be used by a later proposal.
4. On acceptance, atomically remove from source and add to destination. On rejection, make no ownership/state change.

Each output port can transfer at most one item per tick. A source is never revisited. A received item was not part of the gathered source proposals and cannot process, move, ignite, open, or cross another connection until the next tick. The sink is the sole exception: it consumes a valid received item immediately as the transfer commit.

### Phase 3: record

Emit the canonical end-of-tick event list and logical snapshot after all transfer commits. Event order is phase order, then stable node/source/port order, with an explicit deterministic event-kind tie-breaker if needed.

### Duration boundary rule

A duration of `N` ticks is exactly `N` eligible local-update work intervals. If required inputs are present at tick start, a machine may start and perform interval 1 during that tick. Interval `N` completes in that tick's local phase, and the finished output may transfer in the same tick's transfer phase. There is no extra startup or completion tick. Inputs received during transfer cannot contribute an interval until the next tick.

## 7. Extractor contract

There are exactly three configured instances: IronOre, Coal, and PaintCan.

- Unlimited source material.
- After the configured extraction ticks, create one item of the configured resource.
- Hold at most one completed item in the extractor's output slot.
- A blocked item remains the exact same record/ID; it is never deleted, overwritten, duplicated, or moved to an unbounded queue.
- Extraction stalls while the output slot is occupied.
- After dispatch, the next cycle can start and perform its first interval no earlier than the following tick.
- Disabling pauses work and dispatch, preserves state, and causes the node to reject incoming transfers (extractors have no inputs in this topology).

Creation occurs on completion, not on dispatch. Paint-can items are created with the configured color and charge payload.

## 8. Conveyor contract

Each belt exposes Inspector-editable length, speed, capacity, stable IDs, upstream connection, and downstream connection.

- Capacity counts every owned item, including entrance, traveling, and exit-waiting items; it is never exceeded.
- Items are stored in FIFO order and never overtake.
- Progress is integer distance from entrance in `[0, lengthUnits]`.
- Only the front item may leave, and only after reaching `lengthUnits`.
- A blocked front item stays at `lengthUnits`; followers form a deterministic visible queue.
- Belt travel is the operating time. There is no separate production/transfer timer.
- A disabled belt freezes all progress, proposes no output, rejects input, and retains every item and progress value.

### Spacing and movement

For capacity `C` and length `L`, define `spacingUnits = max(1, floor(L / C))`. This permits `C` logical item positions between entrance and exit while leaving a clear insertion gap. Capacity must be positive and `L >= C`, otherwise initialization fails clearly.

During local update, process belt items front-to-back. The front advances by at most `movementPerTick`, clamped to `L`. Each follower advances by at most `movementPerTick`, clamped to no farther than `progressOfItemAhead - spacingUnits` and no lower than its current progress. This produces deterministic queues without overlap.

An incoming item is inserted at progress `0` only when:

- the belt is enabled;
- current count is less than capacity; and
- the belt is empty or the current back item's progress is at least `spacingUnits`.

A same-phase outgoing commit may free count/spacing for a later upstream proposal. Rejected transfers leave the source item untouched.

## 9. Furnace contract

Ports: IronOre input, Coal input, IronBar output.

State:

- One work slot: raw/in-progress ore or completed output bar waiting for dispatch.
- One waiting-coal slot, separate from active fuel.
- Integer smelt intervals completed and integer fuel intervals remaining.

Rules:

- Accept ore only when enabled and the work slot is empty.
- Accept coal only when enabled and the waiting slot is empty. Coal may wait while fuel is active.
- At local-update start, if enabled, fuel is zero, and waiting coal exists, consume that coal, free the waiting slot, emit a coal-consumption event, and set fuel to the configured burn ticks.
- If ore in the slot is unfinished and fuel is positive, complete exactly one smelt work interval, then decrement fuel by one for that same interval. The last fuel tick is usable.
- Waiting coal can ignite at the start of the immediately following tick, with no extra lost work interval.
- Coal received during transfer is first eligible to ignite next tick.
- Fuel burns one tick whenever fuel is active and the machine is enabled, including when no ore exists and when a completed output is blocked. If unfinished ore exists, its one work interval occurs before that tick's fuel decrement.
- On completion, transform the ore into `IronBar` in place, preserving ID, and emit a transformation event.
- A completed blocked bar stays in the work slot; no additional ore is accepted.
- Fuel starvation preserves smelt progress exactly.
- Explicit disable freezes work and fuel, prevents output proposals, rejects both inputs, and preserves all state.

Ordinary starvation and blocked output are status reasons, not the disabled state.

## 10. Coloring station contract

Ports: IronBar input, PaintCan input, PaintedIronBar output.

State:

- One bar work slot containing an unpainted/partial bar or finished blocked output.
- One waiting-can slot.
- One reservoir: color ID and remaining integer charges from the most recently opened can.
- Applied charges on the current bar.
- Partial spray timer/work intervals for the current charge.

Rules:

- Accept a compatible bar only when enabled and the bar slot is empty.
- Accept a compatible can only when enabled, the waiting-can slot is empty, and its color matches the configured run color. The waiting can may coexist with a non-empty reservoir.
- At local-update start, if enabled and reservoir charges are zero, open a compatible waiting can: consume the can, copy all its charges/color to the reservoir, free its slot, and emit a can-open event. Opening costs no work interval and is allowed without a bar.
- Never open another can while reservoir charges remain.
- If an unfinished bar and at least one reservoir charge exist, advance the current charge's spray by one eligible interval.
- Exactly when the configured per-charge spray duration completes, deduct one reservoir charge, increment the bar's applied charges, reset partial spray progress, and emit a charge-application event. At most one charge completes in a tick.
- The next charge begins no earlier than the next tick. If completion empties the reservoir, a waiting can may open at the start of that next tick and spraying may also advance during that tick.
- Do not require a whole bar's worth of paint before spraying. Preserve partial bar and partial charge progress through starvation, pause, disable, and blockage.
- After the required charges are applied, transform the bar to `PaintedIronBar`, preserving ID and configured color, and emit a transformation event.
- A finished blocked bar consumes no more paint and remains in the slot. Opening a waiting can into an empty reservoir is still permitted while output is blocked.
- Explicit disable freezes all local work, proposes no output, rejects both inputs, and preserves all state.

Required accounting example: can A (3 charges) paints bar 1 with 2 and leaves 1; bar 2 uses that 1 plus 1 from can B; can B's remaining 2 paint bar 3. Two cans paint exactly three bars. Without can B, bar 2 waits after one applied charge.

## 11. Sink contract

- Accept only fully painted `PaintedIronBar` items.
- On successful transfer, immediately consume the item; there is no queue or timer.
- Track total consumed, counts by color, and the ordered consumed item IDs.
- Emit a sink-consumption event containing item ID and color.
- Reject every other resource without mutation. No other item may disappear at this node.

## 12. Default scenario

| Setting | Default |
|---|---:|
| Tick duration | 0.05 s |
| Paint color | Blue |
| Ore extraction | 2 s / 40 ticks |
| Coal extraction | 6 s / 120 ticks |
| Paint-can extraction | 4 s / 80 ticks |
| Coal burn | 8 s / 160 ticks |
| Furnace processing | 2 s / 40 fueled ticks |
| Spray per charge | 0.75 s / 15 ticks |
| Charges per can | 3 |
| Charges per bar | 2 |
| Every belt length | 4 world units / 4,000 distance units |
| Every belt speed | 1 world unit/s / 50 distance units per tick |
| Every belt capacity | 4 |
| Default belt spacing | 1,000 distance units |

All values are Inspector-editable and represented in the implementation-independent scenario format. Positive durations, charge counts, capacities, lengths, and speeds are validated.

## 13. Presentation and interaction

The generated demo must be legible and playable without manual wiring:

- Elevated orthographic camera showing the full factory.
- Distinct lanes and direction arrows.
- Labeled machine ports and explicit visual connections.
- Primitive machine models and moving item visuals.
- Ore, coal, cans, bars, and painted bars differ by both shape and color.
- Partially painted bars visibly reflect progress.
- Furnace and sprayer have active feedback.
- A readable panel or world labels expose tick/play state, simulation speed, produced/consumed counters, belt count/capacity, work progress, fuel remaining, queued coal, reservoir color/charges, waiting can, bar paint progress, enabled state, and a specific wait/block reason.
- Controls provide Play, Pause, Reset, Single Tick, simulation speed, and enable/disable toggles for individual nodes.

Provide a saved demo scene, useful reusable prefabs, serialized connections, and materials. If assets cannot be generated directly, provide an idempotent Editor menu command that builds and saves them, document the exact menu click, and do not claim the assets exist until it has been executed successfully. Never fabricate Unity YAML.

## 14. Neutral scenario, snapshot, and events

Keep scenario loading and canonical state extraction behind a small implementation-neutral interface that a later DOTS implementation can satisfy. The MonoBehaviour path remains the sole simulation used by gameplay and tests.

### Scenario format

Use a deterministic, versioned JSON-compatible data model containing:

- format version and exact integer tick duration in microseconds (default `50,000`);
- stable nodes and ports, sorted by ID;
- explicit connections;
- node type, enabled initial state, and all converted integer configuration;
- extractor resource/payload configuration;
- belt length, movement, spacing, and capacity;
- furnace, paint, and sink settings;
- global initial tick, next item ID, counters, and optional explicit initial logical contents/state for adversarial tests.

Serialized JSON field/array ordering is canonical. Loading validates all IDs, connections, values, payloads, ownership uniqueness, and progress bounds before mutating the live simulation.

### Per-tick snapshot

The canonical snapshot includes every value capable of affecting future behavior:

- format version, tick index, integer tick duration in microseconds, run/paused state where relevant, and next item ID;
- node/port IDs and enabled states;
- complete item records, stable IDs, resource, color/charges, and exact owner/slot;
- extractor work and pending output;
- belt FIFO order and integer progress;
- furnace work slot/type/progress, fuel remaining, and waiting coal;
- coloring bar slot/type/applied charges/color/partial spray progress, reservoir color/charges, and waiting can payload;
- sink totals, counts by color, and consumed ID order;
- raw production/consumption, transformation, and paint-charge accounting counters.

Sort nodes, ports, items, maps, and counters by stable ordinal IDs/keys. Exclude GameObject/component references, transforms used only for presentation, wall-clock timestamps, allocation addresses, and non-authoritative interpolation.

### Event stream

Emit a deterministically ordered per-tick stream with explicit tick, event kind, node/port IDs, item ID, resource before/after where applicable, color, payload/count, and connection IDs as applicable. Required kinds:

- item generation;
- successful transfer;
- coal consumption/ignition;
- paint-can opening;
- paint-charge application;
- resource transformation;
- sink consumption.

Rejected transfer attempts may be exposed for diagnostics but, if included canonically, their ordering/schema is also fixed. Snapshots and events serialize without locale-dependent number or key formatting.

## 15. Reset, repeatability, and conservation

Reset reconstructs the exact initialized scenario, including tick zero, allocator, initial item IDs/ownership, progress, timers, enabled states, counters, sink history, and event history. The same scenario and tick count must produce byte-equivalent canonical logical exports whether ticks are stepped singly, scheduled during Play, or requested in batches.

Conservation/accounting must allow tests to prove:

- no item is duplicated or silently lost;
- every transformed output corresponds to one preserved-ID input;
- every consumed/active fuel block corresponds to one generated coal item;
- every applied charge corresponds to one deducted can/reservoir charge;
- only the sink intentionally removes fully painted bars.

## 16. Required automated verification

Tests use Unity Test Framework, run the same MonoBehaviour simulation path, and require no camera/presentation. Use small adversarial scenarios and exact intermediate snapshots, not throughput-only assertions.

1. End-to-end production and sink consumption with correct raw-resource and paint accounting.
2. Full belts and blocked entrances never exceed capacity; stalled outputs preserve ID; unblocking recovers without loss/duplication.
3. Furnace last-fuel-tick work, seamless queued-coal ignition, mid-smelt starvation/resumption, idle/output-blocked continuous burning, and waiting-coal capacity one.
4. Exact two-can/three-bar example, plus bar 2 waiting halfway when can B is absent.
5. Waiting-can/reservoir coexistence, no early opening/discarded leftovers, and exact per-charge duration.
6. Fully painted blocked output consumes no extra paint.
7. Wrong resource/color rejection and immediate valid sink consumption.
8. Pause, single-step, reset, and disabled-node recovery preserve state/progress.
9. Exact post-reset repeatability and equality between individual and differently sized tick batches.
10. Tick boundary proving a newly transferred item cannot move, process, ignite, open, or transfer again at its receiver in the same tick.

Also test duration boundaries (`N` work intervals complete in exactly `N` eligible ticks), deterministic simultaneous creation/transfer ordering, reset of the item-ID allocator, canonical ordering, and invalid scenario/configuration rejection.

## 17. Delivery and acceptance

Implementation is complete only when the repository contains:

- the MonoBehaviour reference simulation and neutral scenario/snapshot interfaces;
- the configured demo topology and presentation;
- saved scene/prefabs/materials, or a verified idempotent generator plus exact invocation steps;
- focused EditMode/PlayMode tests as appropriate;
- a concise README covering startup, controls, file layout, tick/transfer boundary semantics, configuration units/rounding, generator steps if any, and honest verification status.

Run available Unity compilation/tests, fix discovered failures, and report exact verification. If Unity cannot run in the execution environment, identify precisely what remains unverified and give the exact batch command or Editor steps needed. Code inspection alone must never be reported as successful verification.
