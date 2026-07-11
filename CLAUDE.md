# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

EscapeCave (가제) is a 2D Unity game: a procedurally-generated cave escape/platformer where the player character detects the environment via echolocation (sound waves reveal a masked-out dark map) while avoiding monsters that hunt by sound/vibration detection.

- **Engine**: Unity 6 (6000.3.18f1), URP (Universal Render Pipeline)
- **IDE**: Visual Studio 2026 Community
- **Physics**: 2D (Rigidbody2D-based), Mario-style variable-height jump
- **Input**: new Input System (`com.unity.inputsystem`), action asset at `Assets/Project/Data/Input/Player/PlayerControls.inputactions` (generated C# class `PlayerControls.cs`)
- **AI**: `com.unity.behavior` (Unity Behavior Graphs) drives monster AI — not a custom FSM/state machine
- Key packages: `com.unity.behavior`, `com.unity.cinemachine`, `com.unity.2d.tilemap`/`tilemap.extras`, `com.unity.render-pipelines.universal`

There is no CI, build script, or test suite in this repo — this is a Unity Editor project. "Running" the project means opening it in the Unity Editor and pressing Play on one of the scenes under `Assets/Project/Scenes/`.

## Directory structure rules (team convention)

All team-authored resources and code live under `Assets/Project/` only, to avoid mixing with third-party assets (`Assets/ThirdParty/`).

- `Assets/Project/Animations/` — Animator controllers + `.anim` clips, split by `Enemy/` and `Player/`
- `Assets/Project/Audio/` — BGM, SFX
- `Assets/Project/Data/` — ScriptableObject data assets and Input actions, split by domain (`Input/`, `Map/`, `Renderer/`)
- `Assets/Project/Prefabs/` — feature-complete prefabs, organized by system (`Echo/`, `Enemy/`, `Map/`, `Platform/`, `Player/`). Prefer editing prefabs over editing scene-instance overrides directly.
- `Assets/Project/Resources/` — assets that must be loaded dynamically at runtime (`Resources.Load`)
- `Assets/Project/Scenes/` — one scene per feature/owner (see "1 person, 1 scene" rule below); current scenes: `SampleScene`, `Enemy`, `PlayerControlTest`, `RandomMapGeneratorTest`, `EchoSystemTestScene`, `TraceCamera`, `UI`
- `Assets/Project/Scripts/` — C# code (see Architecture below)
- `Assets/Project/Shaders/` — custom shaders / Shader Graphs
- `Assets/Project/Visuals/` — general textures, sprites, materials, tilemap rule/palette assets

When renaming a script file, rename it **inside the Unity Editor**, not on disk, to avoid breaking the `.meta` file's GUID reference.

## Architecture

### Core / Managers (`Assets/Project/Scripts/Core`, `Scripts/Managers`)

- `SingletonBase<T>` (`Core/SingletonBase.cs`) — generic `MonoBehaviour` singleton base (`DontDestroyOnLoad`, auto-creates instance on first access). All manager singletons (`EchoManager`, `PoolManager`, `InventoryManager`) derive from this and live in the `Managers` namespace.
- `PoolManager` — generic `GameObject` object pool keyed by prefab `InstanceID`, built on `UnityEngine.Pool.ObjectPool`. Anything that spawns/despawns repeatedly (echo waves, etc.) should go through `PoolManager.Instance.Pop/Push` instead of `Instantiate`/`Destroy`.
- `EchoManager` — the echolocation "vision" system. `TriggerSound(worldPosition, intensity, speed)` pools an `EchoWaveObject`, puts it (and its children) on a dedicated `EchoWaveMask` layer, and lets it expand; a post-process material (`_postProMaterial`) samples `_echoWaveRT` (a `RenderTexture` mask, `RT_EcholocationMask`) to reveal the world only where the wave has passed. Related: `Scripts/Game/Echo/EchoWaveObject.cs`, `FullscreenEcholocationController.cs`, `RenderTargetEcholocationController.cs`, shader `Shaders/FullScreenEcholocationRT.shader`.
- `InventoryManager` — fixed 3-slot inventory (`slots[]`/`slotIcons[]` `Image[]`), keys 1/2/3 to use an item.

### Player (`Scripts/Game/Player/PlayerController.cs`)

Single monolithic controller (not split into components): movement + Mario-style variable jump (ground detection via collision contact counting, not raycasts), mouse-aimed tongue-grapple attack (`LineRenderer`-driven coroutine `TongueRoutine` that extends toward the cursor, hits `Enemy`/`Item`-tagged colliders, retracts, and calls `InventoryManager.AddItem`), and a "Cry" action. Implements `IEchoable` (`Scripts/Interfaces/IEchoable.cs`) — both attacking and crying call `Echo()`, which triggers `EchoManager.Instance.TriggerSound(...)`. Input is read via the generated `PlayerControls` action asset, wired up in `OnEnable`.

### Monster AI (`Scripts/Game/Monster/`)

Monster behavior is authored as **Unity Behavior Graphs** (visual graph assets, e.g. `Bat.asset`, `Mouse.asset`, plus shared sub-graphs `Common.asset`, `CommonAttack.asset`, `CommonChase.asset`, `CommonHit.asset`), not hand-written state machines. C# in this folder only supplies the graph's custom **Action**/**Condition** node implementations, which are thin adapters that read/write `MonsterController` and `MonsterData`:

- `MonsterController` (`MonsterController.cs`) is the runtime brain each monster GameObject carries: holds `Rigidbody2D`/`SpriteRenderer`/`Animator`/`BehaviorGraphAgent` references, exposes movement primitives (`Move`, `Stop`, `HorizontalPatrol`, `VerticalPatrol`), sensing/range checks (`IsPlayerDetectionRange`, `IsInAttackRange`, `TryResetAggro`), and combat reactions (`TakeDamage`, `StartKnockback`, `StartStun`, `Die`). On `Awake` it pushes itself and initial trigger flags (`SoundTrigger`, `VibTrigger`, `IsDetected`, `IsHit`, `HitReaction`) into the `BehaviorGraphAgent`'s blackboard variables — **the blackboard is the contract between C# and the graph**; adding a new sensed condition means adding both a blackboard variable and a matching `SetVariableValue` call here.
- `MonsterData` (`ScriptableObject`, `Monster/MonsterData/*`) — per-monster-type tunables (detection/attack range, speeds, damage, `HitReaction` enum: `None`/`Die`/`Stun`, knockback/stun durations, `TriggerResponse` flags for Sound/Vibration). New monster types get a new `MonsterData` asset, not new code.
- Action nodes (`*Action.cs`: `IdleAction`, `MoveToTargetAction`, `HorizontalPatrolAction`, `VerticalPatrolAction`, `DashAttackAction`, `StopBeforeAttackAction`, `KnockbackAction`, `StunAction`, `AggroResetAction`, `NoticeAction`, `SpawnAction`, `InitMonsterAction`, `DieAction`, `SoundDetectAction`, `VibrationDetectAction`, `ResetHitTriggerAction`, `ResetSoundTrigger`/`ResetVibTrigger`, `IsTrueAction`) and Condition nodes (`*Condition.cs`: `IsInAttackRangeCondition`) follow the Unity Behavior SDK pattern: `[Serializable, GeneratePropertyBag]` + `[NodeDescription(...)]`/`[Condition(...)]` attributes, `BlackboardVariable<T>` fields wired to the graph, and `OnStart`/`OnUpdate`/`OnEnd` (Actions) or `IsTrue()` (Conditions). When adding a new behavior, mirror an existing node's shape rather than inventing a new pattern.
- Sensing interfaces: `ISoundTrigger`, `IVibrationTrigger` (`Scripts/Interfaces/`) — implemented by things that can be detected by monsters (e.g. player echo/footsteps), separate from `IEchoable` which is the player's own echolocation ability.

### Procedural map generation (`Scripts/Game/MapGenerator/`)

Infinite side-scrolling cave generated in a 3-chunk ring buffer, template-method style:

- `MapGenerator` (drives the system) — owns `_chunks[3]` and `_currentChunkIdx`. Each frame it does a cheap X-position check against the player; once the player crosses the midpoint of the current chunk, `ShiftChunks()` teleports the oldest (now off-screen) chunk to become the new "future" chunk and rebuilds it via `MapChunk.BuildMap`, keeping the tunnel's `_lastExitY` continuous across chunks. `stageRules[]` is an array of `BaseMapRuleSO` indexed by `_currentStageIndex`, so swapping stage themes later means bumping that index — chunk generation logic itself doesn't need to change.
- `MapChunk` — holds the `Tilemap` and reusable `int[,] _mapData`/`_mainPath`; `BuildMap(rule, startY)` clears and delegates generation to the rule SO, returning the tunnel's exit Y for the next chunk to continue from.
- `BaseMapRuleSO` (abstract `ScriptableObject`, template method `GenerateChunk`) — fixed pipeline: `InitializeMap` (fill walls) → `CarveTerrain` (abstract, subclass-defined tunnel carving) → `PlacePaltforms` (virtual, optional) → `RenderToTilemap`. `CaveRuleSO` is the current concrete implementation (cave/mountain theme). New map themes = new `BaseMapRuleSO` subclass overriding `CarveTerrain` (and optionally `PlacePaltforms`), not changes to `MapGenerator`/`MapChunk`.
- `TileDataSO`, `StageManifestSO` — supporting data assets for tile sets and stage ordering.
- Map data encoding: `0` = empty, `1` = wall, `2` = platform (see `BaseMapRuleSO.RenderToTilemap`).

## Coding conventions (team rules)

- **Class / Method / Enum**: `PascalCase`
- **Public fields** (Inspector-exposed): `PascalCase`
- **Private / protected fields**: `_camelCase` (leading underscore required)
- **Local variables / parameters**: `camelCase`
- **Constants**: `UPPER_SNAKE_CASE`
- **Bool names**: prefix with `is`/`has`/`can` (`isDead`, `hasKey`, `canJump`)
- Always use braces `{ }` with their own line, never omit them
- Rename script files from inside the Unity Editor only (protects `.meta` GUIDs)

## Commit convention

Format: `tag: summary`, optionally followed by a blank line and `-`-prefixed detail bullets. Tags (lowercase, followed by `: `):

- `feat` — new feature / new script or asset
- `fix` — bug/error/broken scene-or-prefab fix
- `refactor` — no behavior change, structure/naming/perf cleanup
- `chore` — non-code work (folder structure, package installs, `.gitignore`/README)

Titles are written as direct actions ("구현", "수정", "제거", "추가"), not past-tense narration.

## Git workflow (golden rules)

- **Never push directly to `main`/`dev`.** All work happens on a personal `feature/(feature-name)` branch; merge via PR after review.
- **Pull before starting work** — fetch/pull the main branch and sync before branching or switching.
- **One person, one scene**: prefer building features as prefabs (edited on your own branch) over directly editing shared scenes, except for large work like level design or the main menu.
