# Plan: Extract Debug Overlay as a Unity Package

## Goal

Extract the console, config variables (CVars), commands, and the underlying debug overlay renderer into a standalone Unity package. No "game structure" (`Game.cs`, `BootSequence.cs`, `IGameSystem`, `Stats.cs`, `fpscontroller.cs`) should be part of the package. The user is responsible for putting a single MonoBehaviour in their scene to bootstrap and tick the system.

## Package Layout

```
Packages/com.pandr.unity-debug-overlay/
  package.json
  Runtime/
    com.pandr.unity-debug-overlay.Runtime.asmdef
    DebugOverlay.cs
    DebugOverlayResources.cs
    Console.cs
    CVar.cs
    TextFormatter.cs
    Shaders/
      GlyphShaderProc.shader
      LineShaderProc.shader
    Materials/
      GlyphMaterial.mat
      LineMaterial.mat
    Fonts/
      RobotoMonoFontSheet.tga
    Resources/
      DebugOverlayResources.asset
  Tests/
    com.pandr.unity-debug-overlay.Tests.asmdef
    TextFormatterTests.cs
```

## Changes Required

### 1. Remove `IGameSystem` from `Console`

`Console` currently implements `IGameSystem` (defined in `Game.cs`). Remove this interface from the class declaration. The public API stays the same — `Init()`, `Shutdown()`, `TickUpdate()`, `TickLateUpdate()` — just no interface.

### 2. No bootstrap MonoBehaviour in the package

The package is a pure library. The user is responsible for lifecycle:

- Create `DebugOverlay` and call `Init(width, height)`.
- Create `Console` and call `Init(overlay)`.
- Each frame: call `Console.TickUpdate()`, `Console.TickLateUpdate()`, `DebugOverlay.TickLateUpdate()`.
- End of frame: call `DebugOverlay.Render()`.
- On shutdown: call `Console.Shutdown()` and `DebugOverlay.Shutdown()`.

This keeps execution order entirely in the user's hands.

### 3. Clean up `Console.cs`

- Remove `IGameSystem` interface.
- Remove `using Unity.Collections;` (unused).
- The constructor currently subscribes to `Keyboard.current.onTextInput`. This is fine — it's the New Input System.
- `Init()` currently has two overloads. Collapse into one: `Init(DebugOverlay overlay)`. The parameterless `Init()` that registered the `cvars`/`watch` commands gets folded into the single `Init()`.

### 4. Copy assets with correct GUIDs

Materials, shaders, font texture, and the `Resources/DebugOverlayResources.asset` all reference each other by GUID (via `.meta` files). We must preserve all `.meta` files when moving assets so that Unity references remain intact.

### 5. Assembly definitions

- `Runtime/com.pandr.unity-debug-overlay.Runtime.asmdef` — references `Unity.InputSystem` (needed by Console).
- `Tests/com.pandr.unity-debug-overlay.Tests.asmdef` — references the runtime asmdef and `UnityEngine.TestRunner`/`UnityEditor.TestRunner`.

### 6. `package.json`

```json
{
  "name": "com.pandr.unity-debug-overlay",
  "version": "1.0.0",
  "displayName": "Debug Overlay",
  "description": "In-game debug overlay with console, commands, and config variables (CVars).",
  "unity": "2021.3",
  "dependencies": {
    "com.unity.inputsystem": "1.0.0"
  }
}
```

### 7. Update existing project

After extracting, `Assets/Game/Game.cs` and `Assets/Game/BootSequence.cs` and `Assets/Game/Stats.cs` should be updated to use `DebugOverlaySystem` instead of manually creating `DebugOverlay`/`Console`. The `IGameSystem` interface can stay in Game.cs for the game's own use but Console no longer implements it. `Assets/Demo/fpscontroller.cs` switches from `Game.console` to `DebugOverlaySystem.Console`.

This step is optional — the game code under `Assets/` is not part of the package and can be adapted by the user at their leisure.

## What the user does to integrate

1. Add the package (it lives in `Packages/com.pandr.unity-debug-overlay`).
2. In your bootstrap code, create and init both systems:
   ```csharp
   var overlay = new DebugOverlay();
   overlay.Init(120, 36);
   var console = new Console();
   console.Init(overlay);
   ```
3. Tick them each frame (e.g. from a MonoBehaviour or your own game loop):
   ```csharp
   // Update:      console.TickUpdate();
   // LateUpdate:  console.TickLateUpdate(); overlay.TickLateUpdate();
   // EndOfFrame:  overlay.Render();
   ```
4. Press F12 at runtime to open the console.
5. Declare CVars anywhere in your code: `static CVarFloat myVar = new CVarFloat("myvar", 1.0f, "description");`
6. Add commands: `console.AddCommand("mycmd", MyHandler, "description");`
7. Use the overlay: `DebugOverlay.Write(...)`, `DebugOverlay.DrawGraph(...)`, etc.

## Files NOT included in the package

- `Game.cs`, `BootSequence.cs`, `Stats.cs` — game structure
- `fpscontroller.cs` — demo content
- Scene files, demo assets
