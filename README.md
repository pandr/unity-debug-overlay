# unity-debug-overlay
A fast and (almost) garbage free debug overlay for Unity. The projects contains two primary components: a debug overlay
and a console.

## Performance
Garbage production is minimized by not really using strings a lot and by having convenience functions that mimick string
formatting (using format strings like `"This: {0}"`) known from C#. Rendering happens through the magic of a few procedural draw calls
and is quite fast.

## Debug overlay
The debug overlay is useful for displaying text and graphs that update every frame.
Like this:

![Pretty picture](https://user-images.githubusercontent.com/4175246/28583020-e34a3a12-7167-11e7-8871-7199f410aa8d.gif)

This can be done with some level of convenience using this code:

```c#
    // FPS in top left corner
    DebugOverlay.Write(1, 0, "FPS:{0,6:###.##}", 1.0f / Time.deltaTime);

    // Small graph of FPS below
    fpsHistory[Time.frameCount % fpsHistory.Length] = 1.0f / Time.deltaTime;
    DebugOverlay.DrawGraph(1, 1, 9, 1.5f, fpsHistory, Time.frameCount % fpsHistory.Length, Color.green);
```

Even though it looks like regular string formatting, no garbage will be generated.

## Console
The console is useful for checking logs / output while ingame and also for easily registrering commands
that can be used to tweak the game behaviour or turn on/off debugging aspects.

You can write

```c#
    // Register quit command
    Game.console.AddCommand("quit", CmdQuit, "Quit game");

    /* ... */

    void CmdQuit(string[] args)
    {
    	Game.console.Write("Goodbye\n");
        Application.Quit();
    }
```

and it will work like this:

![Pretty picture](https://user-images.githubusercontent.com/4175246/28582984-d215e5f2-7167-11e7-99ff-e96b2981b9bb.gif)

## Config Variables

The project includes a high-performance config variable system inspired by Quake and Source engines. Config variables can be set from the console and provide O(1) access for use in hot loops.

### Usage

```c#
// Register config variables (typically in Init())
int fovId = ConfigVar.RegisterFloat("fov", 70.0f, "Field of view");
int mouseSensId = ConfigVar.RegisterFloat("mousesens", 1.0f, "Mouse sensitivity");
int showDebugId = ConfigVar.RegisterBool("showdebug", true, "Show debug information");

// Fast access in hot loops
float fov = ConfigVar.GetFloat(fovId);
float mouseSens = ConfigVar.GetFloat(mouseSensId);
bool showDebug = ConfigVar.GetBool(showDebugId);
```

### Console Commands

```
> fov = 90
fov = 90
> mousesens = 2.5
mousesens = 2.5
> showdebug = false
showdebug = False
> cvarlist
Config Variables:
  fov = 90
  mousesens = 2.5
  showdebug = False
> demo
Config Variable Demo:
Try these commands:
  fov = 90
  mousesens = 2.5
  showdebug = false
  graphscale = 2.0
  cvarlist
```


