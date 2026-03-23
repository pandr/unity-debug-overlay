# com.pandr.unity-debug-overlay

An in-game debug overlay with a drop-down console, config variables (CVars), and a GC-free text/graph renderer.

## Installation

In your project's `Packages/manifest.json`:

```json
"com.pandr.unity-debug-overlay": "git@github.com:pandr/unity-debug-overlay.git?path=Packages/com.pandr.unity-debug-overlay"
```

Requires `com.unity.inputsystem`.

## Lifetime wiring

The package is a plain library — it has no MonoBehaviour and no magic initialization. You are responsible for creating, ticking, and shutting down the systems from your own bootstrap code.

### 1. Initialize

```csharp
var overlay = new DebugOverlay();
overlay.Init(120, 36); // width and height in character cells

var console = new Console();
console.Init(overlay);
```

### 2. Tick every frame

```csharp
// Call from Update()
console.TickUpdate();

// Call from LateUpdate()
console.TickLateUpdate();
overlay.TickLateUpdate();

// Call at end of frame (after rendering)
overlay.Render();
```

`Render()` must be called after Unity has finished rendering the frame. The standard way to do this is a coroutine:

```csharp
IEnumerator Start()
{
    var eof = new WaitForEndOfFrame();
    while (true)
    {
        yield return eof;
        overlay.Render();
    }
}
```

### 3. Shutdown

```csharp
console.Shutdown();
overlay.Shutdown();
```

## Console

Press **F12** to open and close the console.

Register commands from anywhere after `Init`:

```csharp
console.AddCommand("quit", args => Application.Quit(), "Quit the application");
```

Execute a command programmatically:

```csharp
console.ExecuteCommand("quit");
```

## CVars

Declare a CVar as a static field anywhere in your codebase — it registers itself automatically:

```csharp
static CVarFloat mySpeed = new CVarFloat("speed", 10.0f, "Player movement speed");
static CVarInt   myLevel = new CVarInt("level", 1, "Current level");
static CVarString myName  = new CVarString("name", "player", "Player name");
```

Read and write in code:

```csharp
float s = mySpeed.value;
mySpeed.value = 20.0f;
```

Read and write from the console at runtime:

```
> speed          // prints current value
> speed 20       // sets value to 20
> cvars          // lists all registered cvars
> watch speed    // shows speed on the overlay every frame
```

Tab completion works for both commands and CVar names.

## Overlay

Write text at a character-grid position:

```csharp
DebugOverlay.Write(0, 0, "FPS: {0}", fps);
DebugOverlay.Write(Color.red, 0, 1, "Health: {0}", health);
```

Draw graphs and histograms:

```csharp
DebugOverlay.DrawGraph(0, 2, 20, 4, samples, startIndex, Color.green);
DebugOverlay.DrawHist(0, 7, 20, 4, samples, startIndex, Color.yellow);
```

Other primitives:

```csharp
DebugOverlay.DrawRect(x, y, w, h, color);
DebugOverlay.DrawLine(x1, y1, x2, y2, color);
DebugOverlay.DrawQuad(x, y, w, h, color);
DebugOverlay.DrawTexturedQuad(x, y, w, h, texture, color);
```

Coordinates are in character cells. The overlay is cleared and re-submitted every frame, so just write what you want to see each `Update`.

## Color markup in text

Inline color codes work in both the console and overlay text using `^RGB` hex notation (one hex digit per channel):

```csharp
console.Write("^F00Error:^FFF something went wrong\n");
DebugOverlay.Write(0, 0, "^0F0OK^FFF all systems go");
```
