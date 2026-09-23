# Fabbrica Bellissima

A deterministic MonoBehaviour factory reference implementation for Unity. The simulation is deliberately separated from presentation so a future DOTS port can be compared tick-for-tick against the same scenarios, snapshots, and events.

## Open and run

1. Open `FabriccaBellissima/` with Unity `6000.3.10f1`.
2. Open `Assets/Scenes/MainGameScene.unity` if it is not already open.
3. Enter Play Mode.

The demo scene and reusable generated assets are already saved. To rebuild them idempotently, click **Tools > Fabbrica Bellissima > Generate Complete Demo**. This rewrites the demo scene, default scenario JSON, materials, and prototype prefabs without manual wiring.

## Controls

The left status panel provides:

- Play/Pause, Single Tick, and Reset;
- simulation speed from 0.1x to 10x (independent of configured belt speed);
- enable/disable toggles for every extractor, belt, machine, and sink;
- raw production, transformation, consumption, fuel, and paint accounting;
- per-node capacity, progress, queue, reservoir, and blocked/waiting reasons.

World labels identify stable nodes and port roles. Resources use different shapes and colors, belt items move continuously, partially painted bars blend toward the configured paint color, and machine beacons pulse while active.

## Deterministic contract

`FactorySimulationCoordinator.StepOneTick()` is the only authoritative advancement path. Machines and belts do not advance state independently.

Each tick has three phases:

1. Nodes update locally in ascending stable node-ID order using start-of-tick state.
2. Eligible outputs are gathered, sorted by stable source node/port ID, and committed atomically. A received item cannot act again until the next tick; the sink consumes valid transfers immediately.
3. Canonical events and the end-of-tick snapshot are emitted.

Positive durations in Inspector seconds convert once using `ceil(seconds / tickDuration)`. Conveyors use 1,000 integer units per world unit; length and movement convert once with decimal arithmetic followed by ceiling. The default 4-unit, 1-unit/second belt at a 0.05-second tick is therefore exactly 4,000 units long and advances 50 units per tick. Rendering reads this state but never feeds values back into it.

Reset restores tick zero, node enabled states, ownership, timers, counters, sink history, and the next global item ID. Advancing the same tick count singly or in arbitrary batches produces identical canonical JSON.

## Configuration and exports

- Inspector authoring: `FactoryDemoAuthoring` on the `Factory Systems` object.
- Portable default config: `Assets/Factory/Generated/DefaultScenario.json`.
- Runtime config type: `FactoryScenario`.
- Neutral simulation API: `IFactorySimulation`.
- Snapshot export: `FactorySimulationCoordinator.ExportCanonicalSnapshot()`.

Scenario JSON stores the tick duration as exact integer microseconds, along with converted integer settings, stable node/port IDs, explicit connections, and optional initial logical state. Canonical snapshots include all node configuration and future-affecting state, item ownership and progress, allocator/counters, sink history, connections, and the deterministic event stream. Presentation transforms and Unity object references are excluded.

## File layout

```text
Assets/Factory/Scripts/       deterministic model, authoring, coordinator, presentation
Assets/Factory/Editor/        idempotent demo generator
Assets/Factory/Generated/     scenario JSON, materials, reusable prefabs
Assets/Factory/Tests/Editor/  adversarial deterministic tests
Assets/Factory/Tests/PlayMode saved-scene runtime smoke test
Assets/Scenes/MainGameScene.unity
```

`FACTORY_SPEC.md` is the authoritative behavior contract; `AGENTS.md` contains repository implementation rules and continuity guidance.

## Verification

From PowerShell, with no other Unity instance holding the project:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\FabriccaBellissima' `
  -runTests -testPlatform EditMode `
  -testResults 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\unity-test-results.xml' `
  -logFile 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\unity-tests.log'

& 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\FabriccaBellissima' `
  -runTests -testPlatform PlayMode `
  -testResults 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\unity-playmode-results.xml' `
  -logFile 'C:\Gamedevolay\DataAnnotation\FabriccaBellissima\unity-playmode.log'
```

Last verified on 2026-09-23 with Unity `6000.3.10f1`:

- EditMode: 16 passed, 0 failed.
- PlayMode: 1 passed, 0 failed.
- Demo generation: completed successfully and saved the scene/prefabs/materials.

When Unity is launched by a sandboxed automation agent, it must be allowed to run outside filesystem isolation so its Licensing Client, Package Manager, and AppData databases can operate. The Unity executable remains scoped to this project path.
