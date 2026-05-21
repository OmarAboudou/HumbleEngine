# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

HumbleEngine is a custom C# UI/application engine, cross-platform (desktop + mobile), with long-term extensibility toward a game engine. Architecture design is complete; the codebase is at the start of the implementation phase.

## Commands

```bash
# Build
dotnet build HumbleEngine.sln

# Run all tests (NUnit)
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Architecture

The engine has two layers:

```
Node Tree   →   structural description (Build())
                    ↓ dirty flags
Render Tree →   layout + paint (SkiaSharp)
```

### Core concepts

**Node** — every UI element is a `Node`. Nodes are mutable and organized as a parent/child tree.

Lifecycle: `constructor` → `Init()` → `Build()` → `Update(delta)` per frame → `Dispose()`.

> `Init()` (not the constructor) is where reflection sets the `Owner` on `Property<T>` fields. Field initializers in subclasses run after the base constructor, so `Property<T>` instances don't exist yet when `Node()` executes.

**Props vs State** — the central distinction:

| | Prop | State |
|--|------|-------|
| Declared on | `Node` directly | `NodeState` subclass |
| Provided by | Parent / ViewModel | The Node itself |
| On rebuild | Re-bound from outside | Survives — persisted by the engine |

**NodeState** — persisted by the engine via the Null Object Pattern. Every `Node` exposes `CreateState()`; stateless nodes return `NodeState.Null` (singleton, no allocation). The engine calls `CreateState()` exactly once per tree position and stores the result only when it's not the null sentinel.

**`Property<T>`** — the single reactivity primitive, replacing `INotifyPropertyChanged`, dirty flags, and bindings. Setting `Value` automatically calls `Owner?.MarkDirty()` and notifies listeners stored as `WeakReference<Action<T>>` (prevents memory leaks when the destination node is destroyed). Supports `BindFrom()` (one-way) and `BindTwoWay()`.

**`Signal` / `Signal<T>`** — event communication inspired by Qt/Godot. `Emit()` is `internal`; only the engine or the owning Node may emit. Consumers use `Connect()` / `Disconnect()`.

**API** — layout nodes expose `Add()` for collection initializer syntax. `NodeExtensions` provides generic fluent methods `On()` and `Bind()`/`BindFrom()` that work with any Node and any Signal/Property, keeping the builder readable by nesting.

**MVVM** — Nodes are the View. ViewModels are plain C# classes with `Property<T>` fields (no `INotifyPropertyChanged`). Models are plain C# classes.

**Dirty flags** — three categories: `_transformDirty` (position/size/layout), `_styleDirty` (color/border/opacity), `_contentDirty` (text/image/data). Triggered automatically by `Property<T>`. Structural rebuilds require an explicit `RequestRebuild()` call.

**Rendering** — SkiaSharp (C# port of Skia) for pixel-perfect cross-platform rendering.

## Key design references

`docs/Learning/synthese-finale.md` — complete architecture specification with code samples for every concept above. Read this before implementing any core type.

`docs/roadmap.md` — architecture decision log (Node Tree from Godot, Signals from Qt/Godot, MVVM from WPF, World/state map from Bevy, SkiaSharp from Flutter).