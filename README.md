# unity-debug-overlay
A fast and garbage free debug overlay for Unity

You can write stuff like

```c#
        DebugOverlay.Write(2, 2, "Hello, {0,-5} world!", Time.frameCount%100<50 ? "Happy" : "Evil");
        DebugOverlay.Write("FrameNo: {0,7}", Time.frameCount);
        DebugOverlay.Write("Time:    {0,7:0000.00}", Time.time);
        DebugOverlay.Write("FPS:     {0,7:###.##}", 1.0f / Time.deltaTime);
        DebugOverlay.Write("MonoHeap:{0,7} kb", (int)(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024));
        DebugOverlay.DrawRect(20, 5.2f, 1.0f/Time.deltaTime*0.1f, 0.6f, Color.green);
        DebugOverlay.Write("PlayerPos: {0,6:000.0} {1,6:000.0} {2,6:000.0}",
            transform.position.x,
            transform.position.y,
            transform.position.z);

        /// Graphing
        System.Array.Copy(fpsArray, 1, fpsArray, 0, fpsArray.Length - 1);
        fpsArray[fpsArray.Length - 1] = 1.0f / Time.deltaTime;
        DebugOverlay.DrawHist(20, 10, 20, 3, fpsArray, Color.blue, 120.0f);

        System.Array.Copy(moveArray, 1, moveArray, 0, moveArray.Length - 1);
        moveArray[moveArray.Length - 1] = transform.parent.position.y;
        DebugOverlay.DrawHist(20, 15, 20, 3, moveArray, Color.red, 4.0f);
```

and it will look like

![Pretty picture](https://user-images.githubusercontent.com/4175246/27818480-0fcb0176-6096-11e7-9962-4371342402f2.png)
