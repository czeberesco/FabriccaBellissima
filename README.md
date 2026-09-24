# Fabbrica Bellissima

A deterministic, modular object-oriented factory reference implementation for Unity. Plain C# node modules and application services are separated from Extenject composition, R3 scheduling, and presentation so new machine types can be registered without changing the central tick pipeline.

## Open and run

1. Open `FabriccaBellissima/` with Unity `6000.3.10f1`.
2. Open `Assets/Scenes/MainGameScene.unity` if it is not already open.
3. Enter Play Mode.

The demo scene and reusable generated assets are already saved. To rebuild them idempotently, click **Tools > Fabbrica Bellissima > Generate Complete Demo**. This rewrites the demo scene, default scenario JSON, materials, and prototype prefabs without manual wiring.

The project pins Extenject `9.3.1` and R3 `1.3.1`. R3's Unity integration is installed through UPM; its official NuGet core assemblies and required BCL assemblies are committed under `Assets/Plugins/R3` because the Unity package intentionally does not contain them.

## Controls

The left status panel provides:

- Play/Pause, Single Tick, and Reset;
- simulation speed from 0.1x to 10x (independent of configured belt speed);
- enable/disable toggles for every extractor, belt, machine, and sink;
- raw production, transformation, consumption, fuel, and paint accounting;
- per-node capacity, progress, queue, reservoir, and blocked/waiting reasons.

World labels identify stable nodes and port roles. Resources use different shapes and colors, belt items move continuously, partially painted bars blend toward the configured paint color, and machine beacons pulse while active.

## Deterministic contract

`FactorySimulationCoordinator.StepOneTick()` delegates to the single application simulation orchestrator and is the only authoritative advancement path. Machines and belts do not advance state independently. R3 observes the Unity player loop only to schedule whole tick requests and publish presentation snapshots; deterministic in-tick traversal remains explicit ordinary C# iteration.

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
Assets/Factory/Scripts/       deterministic core models, node modules/factories, and application services
Assets/Factory/Scripts/Unity/ Extenject/R3 composition, Unity scheduling, controls, authoring, and presentation
Assets/Factory/Editor/        idempotent demo generator
Assets/Factory/Generated/     scenario JSON, materials, reusable prefabs
Assets/Factory/Tests/Editor/  adversarial deterministic tests
Assets/Factory/Tests/PlayMode saved-scene runtime smoke test
Assets/Plugins/R3/            pinned official R3 NuGet runtime assemblies
Assets/Resources/             Extenject ProjectContext prefab and project installer
Assets/Scenes/MainGameScene.unity
```

Extenject lifetime scopes are explicit:

- `ProjectContext` owns stateless cross-scene node factories, their registry, transfer/snapshot services, and `IFactorySimulationFactory`.
- `SceneContext` owns and injects the scene authoring, coordinator, R3-driven session, presentation, and control panel.
- Prefabs remain closed ecosystems and may add `GameObjectContext` only when they acquire local services; the current primitive prototype prefabs do not need one.

Runtime code does not use service locators, singleton managers, scene searches, or `GetComponent` dependency fallbacks. Every independently meaningful production type is in its own source file.

`FACTORY_SPEC.md` is the authoritative behavior contract; `AGENTS.md` contains repository implementation rules and continuity guidance.

## Verification

### Runtime determinism soak

`FactoryDeterminismRuntimeTests` executes five scenarios for 10,000 ticks, 100 times each. Scheduled changes are applied immediately before the named tick's local-update phase. Each rerun must match the first run's exact final canonical JSON and a SHA-256 trace containing every logical event, every toggle, and full-state checkpoints every 250 ticks.

| Scenario | Predefined node outages (`off-on` ticks) |
|---|---|
| Uninterrupted baseline | None |
| Core machines | Iron extractor `400-900`; furnace `2200-2900`; coloring station `5100-5900` |
| Supply and output | Coal extractor `600-1300`; coal belt `1800-2400`; paint extractor `3600-4700`; output belt `7200-8100` |
| Transport and sink | Ore belt `500-1050`; furnace `1700-2300`; bar belt `3100-3900`; paint belt `4800-5650`; sink `7000-8350` |
| Overlapping outage | Iron extractor `300-1200`; coal extractor `450-1750`; coal belt `800-2100`; paint extractor `1500-3300`; coloring station `2600-5200`; output belt `4100-6400` |

The test advances `FactorySimulation.StepOneTick()` directly in a tight loop. It does not use `Time`, frame updates, coroutines, or delays.

From the repository root, with no other Unity instance holding the project:

```powershell
pwsh ./scripts/Run-UnityTests.ps1
```

The helper resolves the repository and Unity project relative to its own location, reads the required editor version from `ProjectSettings/ProjectVersion.txt`, and searches the standard Unity Hub installation locations on Windows, macOS, and Linux. Results are written beneath the repository-local `TestResults/` directory.

For a custom Unity installation, either pass the executable explicitly or define `UNITY_EDITOR`:

```powershell
pwsh ./scripts/Run-UnityTests.ps1 -UnityEditor '/path/to/Unity'

$env:UNITY_EDITOR = '/path/to/Unity'
pwsh ./scripts/Run-UnityTests.ps1 -TestPlatform PlayMode
```

Last verified on 2026-09-24 with Unity `6000.3.10f1`:

- EditMode: 18 passed, 0 failed, including extension-node registration and duplicate-factory rejection.
- PlayMode: 2 passed, 0 failed. This includes five deterministic 10,000-tick scenarios run 100 times each (5,000,000 total ticks), with scheduled three-to-six-node outages and exact canonical-state/trace comparison.
- Demo generation: completed successfully and saved the ProjectContext prefab, DI-wired scene, prefabs, and materials.

When Unity is launched by a sandboxed automation agent, it must be allowed to run outside filesystem isolation so its Licensing Client, Package Manager, and AppData databases can operate. The Unity executable remains scoped to this project path.
