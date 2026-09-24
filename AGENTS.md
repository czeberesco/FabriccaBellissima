# Repository Instructions

## Scope and precedence

- These instructions apply to the entire repository.
- `FACTORY_SPEC.md` is the authoritative product and simulation contract for the factory project. Read it completely before changing Unity assets or C# code.
- The Unity project root is `FabriccaBellissima/`, not the repository root.
- Preserve unrelated user changes. Inspect `git status` before and after work.
- If a later request conflicts with `FACTORY_SPEC.md`, stop and call out the conflict rather than silently changing the contract.

## Current phase gate

- The original simulation specification was approved on 2026-09-23. The modular architecture amendment was approved on 2026-09-24, and implementation is authorized.
- Keep `FACTORY_SPEC.md` synchronized with any later user-approved contract changes.

## Project facts

- Unity editor: `6000.3.10f1`.
- Render pipeline: Universal Render Pipeline (URP) `17.3.0`.
- Test package: Unity Test Framework `1.6.0`.
- Extenject and R3 by Cysharp are required architectural dependencies. Pin their exact installed versions and record their sources in the Unity package manifest/lock files and README.
- Existing scene: `FabriccaBellissima/Assets/Scenes/MainGameScene.unity`.
- Use only APIs compatible with the installed Unity version and packages.
- Prefer built-in Unity features, primitive geometry, UGUI/UI Toolkit, and approved packages. Do not introduce paid assets, unapproved downloads, or third-party simulation frameworks. Extenject and R3 are the only currently approved new runtime dependencies.

## Implementation boundaries

- Build only the object-oriented reference implementation with plain C# domain/application code and MonoBehaviour adapters. Do not use Entities, DOTS, Jobs, Burst, or an external simulation framework.
- Keep authoritative simulation state independent from rendering, physics timing, frame rate, wall-clock time, and GameObject instance IDs.
- A single application-level simulation orchestrator owns tick advancement through an explicit `StepOneTick` method. No station or conveyor may advance authoritative state from a Unity lifecycle callback, R3 subscription, coroutine, animation callback, collision, or trigger.
- Presentation may interpolate but must never feed values back into authoritative state.
- Use explicit serialized connections and stable node/port IDs. Never use proximity, incidental hierarchy/collection order, Unity instance IDs, or randomness to decide simulation outcomes.
- Prefix non-public instance fields with `_`.
- Keep the implementation proportionate: small neutral contracts, ordinary serializable state, and focused components are preferred over an ECS-like architecture or speculative abstraction.

## Architecture and dependency injection

- Apply SOLID boundaries. Domain types own one cohesive behavior; orchestration, validation, transfer resolution, persistence/serialization, presentation mapping, scheduling, and Unity integration are separate responsibilities.
- New node or machine types must be addable through an injected node factory/registry and a focused node implementation. Do not extend a central node-kind switch across simulation services.
- Keep configuration and snapshots extensible: use a stable common envelope plus node-specific configuration/state DTOs and registered mappers instead of adding every machine field to universal records.
- Depend on the smallest useful interfaces. Domain and application code must not depend on concrete presenters, scene objects, Extenject APIs, R3 APIs, or Unity lifecycle methods.
- Do not implement singletons, mutable global state, service locators, static service accessors, `DontDestroyOnLoad` managers, or runtime object searches. Static immutable constants and pure stateless utility functions are not singletons.
- Use Extenject constructor or method injection at composition boundaries. Do not use `GetComponent` fallbacks or `Find*Object*` calls to resolve open-ecosystem dependencies.
- Use a `ProjectContext` installer only for genuinely cross-scene services. Create those services through dedicated factories and bind/inject them from the project context; do not put scene-specific services there for convenience.
- Use a `SceneContext` installer for factory-scene orchestration, scenario/session services, scheduling, UI/presentation adapters, and other scene-lifetime dependencies.
- Use a `GameObjectContext` and local installer when a reusable prefab or specific object benefits from an isolated dependency graph. Closed prefab-internal references may be wired through the Inspector when all referenced objects are owned by that prefab.
- Keep installers declarative. They bind interfaces, factories, configuration, and lifetimes; they do not contain simulation rules or presentation behavior.
- Prefer explicit ownership and disposal. Every R3 subscription belongs to a clearly scoped disposable tied to its ProjectContext, SceneContext, GameObjectContext, or component lifetime.

## R3 and execution loops

- Use R3 for recurring Unity player-loop work, frame/timing streams, UI commands, and presentation observation. Replace simulation-driving `Update`/`FixedUpdate` and presentation polling `LateUpdate` methods with injected R3-based adapters.
- R3 may request ticks only through the single simulation orchestrator. It must not become an alternative authoritative execution path.
- Preserve ordinary deterministic `for`/`foreach` collection traversal inside authoritative simulation ticks. Do not translate ordered tick phases, node visitation, proposal sorting, or transfer commits into reactive operators.
- Do not use R3 scheduling, time operators, concurrency, or unspecified subscription order to determine authoritative results. Wall-clock/player-loop values may decide only how many whole ticks to request.

## Source organization

- Put every independently meaningful class, struct, interface, enum, configuration model, state model, event model, and snapshot model in its own correctly named file. Do not create catch-all files such as `FactoryTypes.cs`.
- A tiny nested private type is allowed only when it is an inseparable implementation detail with no independent contract or reuse value. Default to a separate file when in doubt.
- Organize assemblies/folders so dependencies flow inward: domain and application layers remain independent from Unity presentation and Extenject/R3 adapters. Avoid circular assembly references.

## Determinism and data rules

- Follow the tick phases and boundary behavior in `FACTORY_SPEC.md` exactly.
- Express authoritative durations and conveyor positions as integer tick/distance values after one-time initialization conversion.
- Preserve stable item IDs through ore-to-bar and bar-to-painted-bar transformations.
- Transfers are explicit, deterministic, atomic, all-or-nothing ownership changes. An item may cross at most one connection per tick.
- Stable source node/port order governs transfer resolution. Stable node order governs simultaneous item creation and the global item-ID allocator.
- Reset must restore the entire initial logical state, including IDs, allocator, timers, counters, enabled states, and item ownership.
- Canonical snapshots and events must be deterministically sorted and contain all future-affecting state; never include Unity object references, presentation transforms, timestamps, or allocation addresses.

## Assets and generated setup

- Do not hand-author Unity YAML.
- Prefer an idempotent Editor menu generator for the demo scene, prefabs, materials, wiring, and labels when direct asset creation is unavailable or fragile.
- If a generator is used, document its exact menu path and never claim generated assets exist until the command has actually run and the files have been checked.
- Avoid requiring manual scene wiring. Positions and explicit connections must remain Inspector-editable.

## Verification expectations

- Use the Unity Test Framework and exercise the same domain/application simulation path composed for play mode; do not create a simplified test-only simulator. Pure domain tests may construct explicit collaborators without booting Unity scenes or an Extenject container.
- Keep logic runnable headlessly without a camera or presentation objects.
- Add the focused adversarial coverage listed in `FACTORY_SPEC.md`, including intermediate tick-boundary assertions and conservation/identity checks.
- Add architecture tests or focused integration tests for ProjectContext/SceneContext composition, injected factories, disposal, and the absence of runtime dependency lookup. Prove that an additional test node can be registered without editing central simulation switches.
- Run relevant compilation and tests when Unity is available. Report exact commands, results, and any unverified work honestly; code inspection alone is not a passing test.
- Before handoff, verify that only intended files changed and update the README with startup, controls, layout, units/rounding, tick semantics, and verification status.
