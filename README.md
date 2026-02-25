# unity-debug-overlay
A fast, (almost) garbage-free debug overlay for Unity with three components: a debug overlay, a console, and a CVar system.

Garbage is minimized by avoiding string allocation — formatting uses C#-style format strings (`"This: {0}"`) backed by a procedural renderer with no mesh allocation.

## Debug overlay
Displays text, graphs, and primitives that update every frame:

```c#
// FPS in top left corner
DebugOverlay.Write(1, 0, "FPS:{0,6:###.##}", 1.0f / Time.deltaTime);

// Line graph of FPS below
fpsHistory[Time.frameCount % fpsHistory.Length] = 1.0f / Time.deltaTime;
DebugOverlay.DrawGraph(1, 1, 9, 1.5f, fpsHistory, Time.frameCount % fpsHistory.Length, Color.green);
```

![Debug overlay](https://user-images.githubusercontent.com/4175246/28583020-e34a3a12-7167-11e7-8871-7199f410aa8d.gif)

Additional drawing calls: `DrawHist`, `DrawRect`, `DrawLine`, `DrawQuad`, `DrawTexturedQuad`, `SetColor`, `SetOrigin`. Text supports inline color markup via `^RGB` (e.g. `^F00` for red).

## Console
Toggle with **F12**. Supports command history (up/down), tab completion, and mouse scroll.

```c#
Game.console.AddCommand("quit", CmdQuit, "Quit game");

void CmdQuit(string[] args)
{
    Game.console.Write("Goodbye\n");
    Application.Quit();
}
```

![Console](https://user-images.githubusercontent.com/4175246/28582984-d215e5f2-7167-11e7-99ff-e96b2981b9bb.gif)

Built-in commands: `help`, `dump` (scene hierarchy), `cvars`, `watch`.

## CVars
Typed config variables readable and settable from the console at runtime:

```c#
static CVarFloat showFps = new CVarFloat("showfps", 0, "Show FPS counter");

// In update:
if (showFps.value > 0)
    DebugOverlay.Write(1, 0, "FPS:{0,6:###.##}", 1.0f / Time.deltaTime);
```

In the console: type `showfps` to read, `showfps 1` to set. Use `watch showfps` to display it on the overlay continuously. Types supported: `CVarFloat`, `CVarInt`, `CVarString`.
