using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OverlayTest : MonoBehaviour
{
    float[] fpsArray = new float[200];
    float[] frameTimeArray = new float[100];

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_StopWatchFreq;
    long m_LastFrameTicks;
    DebugOverlay m_DebugOverlay;
    Console m_Console;

    void Awake()
    {
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatchFreq = System.Diagnostics.Stopwatch.Frequency;
        m_StopWatch.Start();
        m_LastFrameTicks = m_StopWatch.ElapsedTicks;
        Debug.Assert(System.Diagnostics.Stopwatch.IsHighResolution);
        m_DebugOverlay = GetComponent<DebugOverlay>();
        m_DebugOverlay.Init();
        m_Console = new Console();
        m_Console.Init(DebugOverlay.Width, DebugOverlay.Height);
        m_Console.AddCommand("quit", CmdQuit, "Quit game");
    }

    void CmdQuit(string[] args)
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    void OnDestroy()
    {
        m_DebugOverlay.Shutdown();
        m_DebugOverlay = null;
        m_Console = null;
    }

    int dataIndex = 0;
    float[] dataWindow = new float[10];

    void LogData(float d)
    {
        dataWindow[dataIndex] = d;
        dataIndex = (dataIndex + 1) % dataWindow.Length;
    }

    void CalcStatistics(float[] data, int count, out float mean, out float variance, out float minValue, out float maxValue)
    {
        float sum = 0, sum2 = 0;
        minValue = float.MaxValue;
        maxValue = float.MinValue;
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
    void Update()
    {
        long ticks = m_StopWatch.ElapsedTicks;
        float frameDurationMs = (ticks - m_LastFrameTicks) * 1000 / (float)m_StopWatchFreq;
        m_LastFrameTicks = ticks;
        DebugOverlay.SetColor(Color.yellow);
        DebugOverlay.SetOrigin(2, 2);
        DebugOverlay.Write(0, 0, "Hello, {0,-5} world!", Time.frameCount % 100 < 50 ? "Happy" : "Evil");
        DebugOverlay.Write(0, 1, "FrameNo: {0,7}", Time.frameCount);
        DebugOverlay.Write(0, 2, "FPS:     {0,7:###.##}", 1.0f / Time.deltaTime);
        DebugOverlay.Write(0, 3, "MonoHeap:{0,7} kb", (int)(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024));
        DebugOverlay.Write(0, 4, "PlayerPos: {0,6:000.0} {1,6:000.0} {2,6:000.0}",
            transform.position.x,
            transform.position.y,
            transform.position.z);

        /// Graphing
        DebugOverlay.SetOrigin(0, 0);
        float fps = Time.deltaTime * 1000.0f;
        var idx = (Time.frameCount * 2) % fpsArray.Length; ;
        fpsArray[idx] = -Mathf.Min(0,frameDurationMs - fps);
        fpsArray[idx+1] = Mathf.Max(0,frameDurationMs - fps);
        float variance, mean, min, max;
        CalcStatistics(fpsArray, fpsArray.Length, out mean, out variance, out min, out max);
        DebugOverlay.DrawHist(20, 10, 20, 3, fpsArray, Time.frameCount, colors, 2, max);
        DebugOverlay.SetColor(Color.red);
        DebugOverlay.Write(20, 14, "{0} ({1} +/- {2})", frameDurationMs-fps, mean, Mathf.Sqrt(variance));

        DebugOverlay.DrawGraph(45, 10, 40, 3, fpsArray, Time.frameCount, colors, 2, max);

        var idx2 = Time.frameCount % frameTimeArray.Length;
        frameTimeArray[idx2] = frameDurationMs;
        CalcStatistics(frameTimeArray, frameTimeArray.Length, out mean, out variance, out min, out max);
        DebugOverlay.DrawHist(20, 15, 20, 3, frameTimeArray, Time.frameCount, Color.red, max);
        // Draw a 'scale' line
        float scale = 18.0f - 3.0f / max * 16.6667f;
        DebugOverlay.DrawLine(20, scale, 40, scale, Color.black);
        DebugOverlay.Write(20, 18, "{0} ({1} +/- {2})", frameDurationMs, mean, Mathf.Sqrt(variance));

        DebugOverlay.DrawGraph(45, 15, 40, 3, frameTimeArray, Time.frameCount, Color.red, max);

        for (var i = 0; i < 30; i++)
            DebugOverlay.DrawLine(20, 20, 20 + Mathf.Sin(i) * 10, 20 + Mathf.Cos(i) * 10, Color.black);

        if (Time.frameCount % 100 == 0)
            m_Console.Write(".");

        m_Console.TickUpdate();
    }


    void LateUpdate()
    {
        m_Console.TickLateUpdate();
        m_DebugOverlay.TickLateUpdate();
    }
}
