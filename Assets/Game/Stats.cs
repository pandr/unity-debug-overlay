using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : IGameSystem
{

    float[][] fpsArray = new float[2][] { new float[100], new float[100] };
    float[] frameTimeArray = new float[100];

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_StopWatchFreq;
    long m_LastFrameTicks;

    int m_ShowStats = 1;

    public void Init()
    {
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatchFreq = System.Diagnostics.Stopwatch.Frequency;
        m_StopWatch.Start();
        m_LastFrameTicks = m_StopWatch.ElapsedTicks;
        Debug.Assert(System.Diagnostics.Stopwatch.IsHighResolution);
        Game.console.AddCommand("showstats", CmdShowstats, "Show or hide stats");
    }

    private void CmdShowstats(string[] args)
    {
        m_ShowStats = (m_ShowStats + 1) % 3;
    }

    void CalcStatistics(float[] data, out float mean, out float variance, out float minValue, out float maxValue)
    {
        float sum = 0, sum2 = 0;
        minValue = float.MaxValue;
        maxValue = float.MinValue;
        int count = data.Length;
        for (var i = 0; i < count; i++)
        {
            var x = data[i];
            sum += x;
            if (x < minValue) minValue = x;
            if (x > maxValue) maxValue = x;

        }
        mean = sum / count;
        for (var i = 0; i < count; i++)
        {
            float d = data[i] - mean;
            sum2 += d * d;
        }
        variance = sum2 / (count - 1);
    }

    static Color[] colors = new Color[] { Color.red, Color.green };
    float[] fpsHistory = new float[50];
    public void TickUpdate()
    {
        if (m_ShowStats < 1)
            return;

        long ticks = m_StopWatch.ElapsedTicks;
        float frameDurationMs = (ticks - m_LastFrameTicks) * 1000 / (float)m_StopWatchFreq;
        m_LastFrameTicks = ticks;

        DebugOverlay.SetColor(Color.yellow);
        DebugOverlay.SetOrigin(0, 0);

        DebugOverlay.Write(1, 0, "FPS:{0,6:###.##}", 1.0f / Time.deltaTime);
        fpsHistory[Time.frameCount % fpsHistory.Length] = 1.0f / Time.deltaTime;
        DebugOverlay.DrawGraph(1, 1, 9, 1.5f, fpsHistory, Time.frameCount % fpsHistory.Length, Color.green);

        DebugOverlay.Write(30, 0, "Open console (F12) and type: \"showstats\" to toggle graphs");
      
        if (m_ShowStats < 2)
            return;

        DebugOverlay.Write(0, 4, "Hello, {0,-5} world!", Time.frameCount % 100 < 50 ? "Happy" : "Evil");
        DebugOverlay.Write(0, 5, "FrameNo: {0,7}", Time.frameCount);
        DebugOverlay.Write(0, 6, "MonoHeap:{0,7} kb", (int)(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024));

        /// Graphing difference between deltaTime and actual passed time
        float fps = Time.deltaTime * 1000.0f;
        var idx = Time.frameCount % fpsArray[0].Length; ;
        fpsArray[0][idx] = -Mathf.Min(0, frameDurationMs - fps);
        fpsArray[1][idx] = Mathf.Max(0, frameDurationMs - fps);
        float variance, mean, min, max;
        CalcStatistics(fpsArray[0], out mean, out variance, out min, out max);

        // Draw histogram over time differences
        DebugOverlay.DrawHist(20, 10, 20, 3, fpsArray, Time.frameCount, colors, max);
        DebugOverlay.SetColor(new Color(1.0f,0.3f,0.0f));
        DebugOverlay.Write(20, 14, "{0,4:#.###} ({1,4:##.#} +/- {2,4:#.##})", frameDurationMs - fps, mean, Mathf.Sqrt(variance));

        DebugOverlay.DrawGraph(45, 10, 40, 3, fpsArray, Time.frameCount, colors, max);

        /// Graphing frametime
        var idx2 = Time.frameCount % frameTimeArray.Length;
        frameTimeArray[idx2] = frameDurationMs;
        CalcStatistics(frameTimeArray, out mean, out variance, out min, out max);
        DebugOverlay.DrawHist(20, 15, 20, 3, frameTimeArray, Time.frameCount, Color.red, max);

        // Draw legend
        float scale = 18.0f - 3.0f / max * 16.6667f;
        DebugOverlay.DrawLine(20, scale, 40, scale, Color.black);
        DebugOverlay.Write(20, 18, "{0,5} ({1} +/- {2})", frameDurationMs, mean, Mathf.Sqrt(variance));

        DebugOverlay.DrawGraph(45, 15, 40, 3, frameTimeArray, Time.frameCount, Color.red, max);

        // Draw some lines to help visualize framerate fluctuations
        float ratio = (float)DebugOverlay.Height / DebugOverlay.Width * Screen.width / Screen.height;
        float time = (float)Time.frameCount / 60.0f;
        for (var i = 0; i < 10; i++)
            DebugOverlay.DrawLine(60, 20, 60 + Mathf.Sin(Mathf.PI*0.2f*i + time) * 8.0f, 20 + Mathf.Cos(Mathf.PI*0.2f*i + time) * 8.0f * ratio, Color.black);
    }

    public void Shutdown() { }
}
