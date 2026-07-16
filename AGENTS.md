# Valor Chronicle — Codex Development Instructions

## Project overview

Valor Chronicle is a portrait-oriented Unity Match-3 boss battle RPG.

* Engine: Unity
* Primary language: C#
* Target platforms: Windows, macOS, Android
* Screen orientation: Portrait
* UI reference resolution: 1080 × 1920
* Canvas scaling mode: Scale With Screen Size / Expand
* Puzzle board: 6 columns × 5 rows
* Standard battle length: 25 turns
* Maximum party size: 5 characters
* Empty party slots are allowed

## General working rules

1. Inspect the existing project structure and related files before making changes.
2. Do not assume game mechanics that are not explicitly confirmed.
3. When a mechanic or requirement is ambiguous, explain the ambiguity before encoding a permanent rule.
4. Keep changes limited to the requested task.
5. Do not edit unrelated files for cleanup or stylistic consistency.
6. Prefer small, independently reviewable changes.
7. Preserve all Unity `.meta` files.
8. Never manually modify generated Unity folders or files such as:

   * `Library/`
   * `Temp/`
   * `Logs/`
   * `Obj/`
   * generated `.csproj`, `.sln`, or `.slnx` files
9. Do not install or remove Unity packages unless explicitly requested.
10. Do not commit, push, merge, rebase, or modify Git history unless explicitly requested.

## Required pre-edit report

Before editing files, briefly report:

* Files to add
* Files to modify
* Reason each file needs to change
* Any unresolved design or implementation decisions
* Whether scenes, prefabs, ScriptableObjects, packages, or ProjectSettings would be affected

Do not begin broad or destructive changes before reporting their impact.

A single isolated script addition may proceed after a short impact report when the requirements are unambiguous.

## Unity architecture

### Core game logic

* Prefer plain C# classes for deterministic game rules and calculations.
* Use `MonoBehaviour` only when Unity lifecycle, GameObjects, scenes, input, UI, animation, audio, or presentation requires it.
* Avoid putting unrelated systems into a single manager.
* Do not create a giant `CombatManager`, `GameManager`, or `UIManager`.
* Controllers should coordinate systems rather than contain every calculation.
* Prefer explicit dependencies over global static state or scene searches.
* Avoid repeated use of `FindObjectOfType`, `GameObject.Find`, or similar runtime lookup patterns.

### Data separation

Keep the following categories separate:

1. Content definitions

   * ScriptableObjects or external content data
   * Character, boss, relic, skill, and reward definitions

2. Runtime state

   * Plain C# battle and session state
   * Discarded after the relevant session ends

3. Save data

   * Serializable DTOs containing only persistent player progress

Do not mutate ScriptableObject definition assets during runtime.

Do not save derived values when they can be recalculated from stable saved data.

Use stable internal IDs for content references.

## UI and resolution rules

* The UI reference resolution is 1080 × 1920.
* The Canvas Scaler uses `Scale With Screen Size` and `Expand`.
* Important interactive UI belongs under `SafeAreaRoot`.
* Full-screen backgrounds, fades, and input blockers may extend outside the Safe Area.
* Safe Area calculations must use `Screen.safeArea`.
* Do not use hardcoded notch, status-bar, or gesture-area sizes.
* Do not modify the Main Camera viewport, orthographic size, or aspect handling unless explicitly requested.
* Use RectTransform anchors and layout rules instead of relying only on fixed coordinates.
* The 6 × 5 puzzle board must preserve square cells at every supported aspect ratio.
* UI code must not directly mutate battle or save data.
* Prefer event-driven UI updates over polling the complete battle state every frame.

## Confirmed gameplay rules

### Party

* Party slots have a fixed left-to-right order.
* A party contains up to 5 characters.
* Empty party slots are allowed.
* Empty slots are skipped when resolving character actions.

### Match processing

* The board contains 6 columns and 5 rows.
* A move swaps one selected block with an adjacent block.
* A swap that creates no valid match is reverted.
* An invalid swap does not consume a turn.
* Match events are stored separately rather than merged by element.
* Match events resolve in the order in which the block groups were cleared.
* Characters corresponding to the event element act from the leftmost party slot to the rightmost slot.

### Character stat growth

Character HP and ATK use direct linear interpolation between level 1 and level 100.

* Calculate each level directly from the level 1 and level 100 values.
* Do not accumulate a rounded per-level increment.
* Keep floating-point precision during the calculation.
* Round only the final result.
* Use standard half-up behavior for positive stats, such as:
  `Math.Round(value, MidpointRounding.AwayFromZero)`.

### Shields

Shields remain separate runtime instances.

When incoming damage consumes shields:

1. Consume the shield with the shortest remaining duration first.
2. When remaining duration is equal, consume the shield created earlier first.
3. Use ascending creation order as the deterministic tie-breaker.

## Code quality

* Follow the namespace and folder conventions already present in the project.
* If no convention exists, propose one before introducing a large structure.
* Use clear names that describe game-domain intent.
* Keep classes focused on one responsibility.
* Validate public or serialized input where invalid data could corrupt state.
* Avoid unnecessary abstractions for a feature that currently has only one simple implementation.
* Add interfaces only when they represent an actual boundary or multiple implementations are expected.
* Use comments to explain non-obvious intent, not to repeat the code.
* Avoid allocations in frequently executed battle or board loops when reasonably practical.
* Do not optimize prematurely at the cost of clarity.

## Logging rules

* Project-owned runtime code must use `ValorChronicle.Core.Logging.GameLogger` instead of calling `UnityEngine.Debug` directly.
* Use `GameLogger.Log` for normal progress and diagnostic information.
* Use `GameLogger.Warning` for development-time warnings that do not represent runtime failure.
* Use `GameLogger.Error` for failure states that must remain visible in release builds.
* Use `GameLogger.Exception` for caught exceptions that must remain visible in release builds.
* Do not conditionally remove initialization failures, missing required services, scene-load failures, or caught exceptions from release builds.
* Do not apply `ConditionalAttribute` to Unity lifecycle methods.

## Content data rules

* Content definitions use ScriptableObject assets as immutable source data.
* Runtime state must not be stored in ScriptableObject definitions.
* All content references use stable internal IDs, not display names.
* Content IDs use lowercase letters, digits, and single underscores only.
* Once an ID is used in save data, do not rename it without migration.
* Validate all definitions before building runtime lookup dictionaries.
* Derived values such as final HP or final ATK must not be stored in definitions or save data.

## Random rules

* Project-owned gameplay code must not call `UnityEngine.Random` directly.
* Use `IRandomSource` for gameplay randomness.
* Use `UnityRandomSource` for normal runtime behavior.
* Use `SeededRandomSource` for deterministic tests and bug reproduction.

## Scenes, prefabs, and assets

Do not modify scenes, prefabs, imported assets, ScriptableObjects, or ProjectSettings unless the request explicitly requires it.

When such changes are required:

* State exactly which asset will change.
* Explain why a script-only solution is insufficient.
* Avoid rewriting unrelated serialized fields.
* Confirm that corresponding `.meta` files remain intact.

Do not generate placeholder art, audio, fonts, or packages unless explicitly requested.

## Validation and testing

After implementation:

1. Check for C# compile errors.
2. Check for relevant compiler warnings.
3. Run available tests related to the changed code.
4. When automated Unity tests do not exist, provide exact Unity Editor verification steps.
5. Test normal cases, edge cases, and invalid input relevant to the task.
6. Confirm that unrelated runtime state or assets were not changed.
7. Do not claim that Unity Editor or device testing was completed unless it was actually performed.

For deterministic logic, prefer tests that can use a fixed random seed or injected random source.

## Completion report

After completing a task, report:

* Added files
* Modified files
* Deleted files
* Summary of behavior implemented
* How the implementation was validated
* Unity Editor setup steps, if any
* Known limitations or untested areas
* Any follow-up work that is necessary but intentionally outside the current task

When no existing file needed modification, state that explicitly.

## Current development priority

The project is at the initial setup stage.

Current priorities are:

1. Repository and development environment setup
2. Portrait resolution and Safe Area handling
3. Project-wide foundation and folder structure
4. Save-data skeleton
5. Match-3 board implementation
6. Battle flow and state machine

Do not implement later systems prematurely unless explicitly requested.
