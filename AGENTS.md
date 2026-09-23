# Repository Instructions

## Scope and precedence

- These instructions apply to the entire repository.
- `FACTORY_SPEC.md` is the authoritative product and simulation contract for the factory project. Read it completely before changing Unity assets or C# code.
- The Unity project root is `FabriccaBellissima/`, not the repository root.
- Preserve unrelated user changes. Inspect `git status` before and after work.
- If a later request conflicts with `FACTORY_SPEC.md`, stop and call out the conflict rather than silently changing the contract.

## Current phase gate

- The specification was approved on 2026-09-23 and implementation is authorized.
- Keep `FACTORY_SPEC.md` synchronized with any later user-approved contract changes.

## Project facts

- Unity editor: `6000.3.10f1`.
- Render pipeline: Universal Render Pipeline (URP) `17.3.0`.
- Test package: Unity Test Framework `1.6.0`.
- Existing scene: `FabriccaBellissima/Assets/Scenes/MainGameScene.unity`.
- Use only APIs compatible with the installed Unity version and packages.
- Prefer built-in Unity features, primitive geometry, UGUI/UI Toolkit, and existing packages. Do not introduce paid assets, downloads, or third-party simulation frameworks.

## Implementation boundaries

- Build only the traditional MonoBehaviour/object-oriented reference implementation. Do not use Entities, DOTS, Jobs, Burst, or an external simulation framework.
- Keep authoritative simulation state independent from rendering, physics timing, frame rate, wall-clock time, and GameObject instance IDs.
- A single coordinator owns tick advancement through an explicit `StepOneTick` method. No station or conveyor may advance authoritative state in `Update`, `FixedUpdate`, coroutines, animation callbacks, collisions, or triggers.
- Presentation may interpolate but must never feed values back into authoritative state.
- Use explicit serialized connections and stable node/port IDs. Never use proximity, incidental hierarchy/collection order, Unity instance IDs, or randomness to decide simulation outcomes.
- Prefix non-public instance fields with `_`.
- Keep the implementation proportionate: small neutral state/configuration interfaces, ordinary serializable state, and focused components are preferred over an ECS-like architecture.

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

- Use the Unity Test Framework and exercise the same MonoBehaviour simulation path used by play mode; do not create a simplified test-only simulator.
- Keep logic runnable headlessly without a camera or presentation objects.
- Add the focused adversarial coverage listed in `FACTORY_SPEC.md`, including intermediate tick-boundary assertions and conservation/identity checks.
- Run relevant compilation and tests when Unity is available. Report exact commands, results, and any unverified work honestly; code inspection alone is not a passing test.
- Before handoff, verify that only intended files changed and update the README with startup, controls, layout, units/rounding, tick semantics, and verification status.
