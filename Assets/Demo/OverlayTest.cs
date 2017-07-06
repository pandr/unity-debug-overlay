using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayTest : MonoBehaviour
{
    float[] fpsArray = new float[100];
    float[] frameTimeArray = new float[100];

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_StopWatchFreq;
    long m_LastFrameTicks;

    void Awake()
    {
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatchFreq = System.Diagnostics.Stopwatch.Frequency;
        m_StopWatch.Start();
        m_LastFrameTicks = m_StopWatch.ElapsedTicks;
        Debug.Assert(System.Diagnostics.Stopwatch.IsHighResolution);
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

    void Update()
    {
        long ticks = m_StopWatch.ElapsedTicks;
        float frameDurationMs = (ticks - m_LastFrameTicks) * 1000 / (float)m_StopWatchFreq;
        m_LastFrameTicks = ticks;
        DebugOverlay.Write(2, 2, "Hello, {0,-5} world!", Time.frameCount % 100 < 50 ? "Happy" : "Evil");
        DebugOverlay.Write("FrameNo: {0,7}", Time.frameCount);
        DebugOverlay.Write("FPS:     {0,7:###.##}", 1.0f / Time.deltaTime);
        DebugOverlay.Write("MonoHeap:{0,7} kb", (int)(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024));
        DebugOverlay.DrawRect(20, 5.2f, 1.0f / Time.deltaTime * 0.1f, 0.6f, Color.green);
        DebugOverlay.Write("PlayerPos: {0,6:000.0} {1,6:000.0} {2,6:000.0}",
            transform.position.x,
            transform.position.y,
            transform.position.z);

        /// Graphing
        System.Array.Copy(fpsArray, 1, fpsArray, 0, fpsArray.Length - 1);
        float fps = Time.deltaTime * 1000.0f;
        fpsArray[fpsArray.Length - 1] = frameDurationMs - fps;
        float variance, mean, min, max;
        CalcStatistics(fpsArray, fpsArray.Length, out mean, out variance, out min, out max);
        DebugOverlay.DrawHist(20, 10, 20, 3, fpsArray, Color.blue, max);
        DebugOverlay.Write(20, 14, "{0} ({1} +/- {2})", frameDurationMs-fps, mean, Mathf.Sqrt(variance));

        System.Array.Copy(frameTimeArray, 1, frameTimeArray, 0, frameTimeArray.Length - 1);
        frameTimeArray[frameTimeArray.Length - 1] = frameDurationMs;
        CalcStatistics(frameTimeArray, frameTimeArray.Length, out mean, out variance, out min, out max);
        DebugOverlay.DrawHist(20, 15, 20, 3, frameTimeArray, Color.red, max);
        DebugOverlay.DrawRect(20, 18.0f - 3.0f/max*16.6667f, 20, 0.1f, Color.black);
        DebugOverlay.Write(20, 18, "{0} ({1} +/- {2})", frameDurationMs, mean, Mathf.Sqrt(variance));

    }
}
