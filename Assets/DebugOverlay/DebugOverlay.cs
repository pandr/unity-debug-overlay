using UnityEngine;
using System.Collections;

public class DebugOverlay : MonoBehaviour
{
    [Header("Overlay size")]
    public int width = 80;
    public int height = 25;

    [Header("Font material info")]
    public Material instanceMaterialProc;
    public int charCols = 30;
    public int charRows = 16;
    public int cellWidth = 32;
    public int cellHeight = 32;

    // Mono stuff
    void Start() { Init(); }
    void Update() { Tick(); }
    void OnDisable() { DeInit(); }

    public static DebugOverlay instance;

    public void Init()
    {
        instance = this;
    }

    public static void Write(float x, float y, string format)
    {
        instance._Write(x, y, format, new ArgList0());
    }
    public static void Write(string format)
    {
        instance._Write(format, new ArgList0());
    }

    public static void Write<T>(float x, float y, string format, T arg)
    {
        instance._Write(x, y, format, new ArgList1<T>(arg));
    }

    public static void Write<T>(string format, T arg)
    {
        instance._Write(format, new ArgList1<T>(arg));
    }
    public static void Write<T0, T1>(float x, float y, string format, T0 arg0, T1 arg1)
    {
        instance._Write(x, y, format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public static void Write<T0, T1>(string format, T0 arg0, T1 arg1)
    {
        instance._Write(format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public static void Write<T0, T1, T2>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        instance._Write(x, y, format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
    }
    public static void Write<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    {
        instance._Write(format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
    }

    // Draw a stacked histogram from numSets of data. Data must contain numSets of interleaved, non-negative datapoints.
    public static void DrawHist(float x, float y, float w, float h, float[] data, Color[] color, int numSets, float maxRange = -1.0f)
    {
        instance._DrawHist(x, y, w, h, data, color, numSets, maxRange);
    }

    static Color[] m_Colors = new Color[1];
    public static void DrawHist(float x, float y, float w, float h, float[] data, Color color, float maxRange = -1.0f)
    {
        m_Colors[0] = color;
        instance._DrawHist(x, y, w, h, data, m_Colors, 1, maxRange);
    }

    public static void DrawRect(float x, float y, float w, float h, Color col)
    {
        instance._DrawRect(x, y, w, h, col);
    }

    void _DrawText(float x, float y, ref char[] text, int length, Color color)
    {
        var idx = AllocQuads(length);
        QuadData qd;
        qd.color = color;
        for(var i = 0; i < length; i++)
        {
            qd.character = text[i];
            qd.rect = new Vector4(x + i, y, 1, 1);
            m_QuadDatas[idx + i] = qd;
        }        
    }

    void _DrawHist(float x, float y, float w, float h, float[] data, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (data.Length % numSets != 0)
            throw new System.ArgumentException("Length of data must be a multiple of numSets");
        if (color.Length != numSets)
            throw new System.ArgumentException("Length of colors must be numSets");

        var dataLength = data.Length;
        var numSamples = dataLength / numSets;
        var idx = AllocQuads(numSets * numSamples);

        float maxData = float.MinValue;

        for (var i = 0; i < numSamples; i++)
        {
            float sum = 0;

            for (var j = 0; j < numSets; j++)
                sum += data[i * numSets + j];

            if (sum > maxData) maxData = sum;
        }

        if (maxData > maxRange)
            maxRange = maxData;
        float minRange = 0;

        float dx = w / numSamples;
        float scale = maxRange > minRange ? h / (maxRange - minRange) : 1.0f;

        QuadData qd;
        qd.character = '\0';
        float stackOffset = 0;
        for (var i = 0; i < dataLength; i++)
        {
            var set = i % numSets;
            if(set == 0)
                stackOffset = 0;
            var c = color[set];
            qd.color.x = c.r;
            qd.color.y = c.g;
            qd.color.z = c.b;
            qd.color.w = c.a;
            float d = data[i];
            float scaledData = d * scale; // now in [0, h]
            qd.rect.x = x + dx * i;
            qd.rect.y = y + h - d * scale - stackOffset;
            qd.rect.z = dx;
            qd.rect.w = d * scale;
            stackOffset += scaledData;
            m_QuadDatas[idx++] = qd;
        }
    }

    void _DrawRect(float x, float y, float w, float h, Color col)
    {
        QuadData rd;
        rd.rect = new Vector4(x, y, w, h);
        rd.color = col;
        var idx = AllocQuads(1);
        rd.character = '\0';
        m_QuadDatas[idx] = rd;
    }

    void _Clear()
    {
        m_NumQuadsUsed = 0;
    }

    void _Write<T>(string format, T argList) where T : IArgList
    {
        _Write(m_LastWriteX, m_LastWriteY + 1, format, argList);
    }

    char[] _buf = new char[1024];
    void _Write<T>(float x, float y, string format, T argList) where T : IArgList
    {
        var num = StringFormatter.__Write<T>(ref _buf, 0, format, argList);
        _DrawText(x, y, ref _buf, num, m_CurrentColor);
        m_LastWriteX = x;
        m_LastWriteY = y;
    }

    int AllocQuads(int num)
    {
        var idx = m_NumQuadsUsed;
        m_NumQuadsUsed += num;
        if (m_NumQuadsUsed > m_QuadDatas.Length)
        {
            var newRects = new QuadData[m_NumQuadsUsed + 128];
            System.Array.Copy(m_QuadDatas, newRects, m_QuadDatas.Length);
            m_QuadDatas = newRects;
        }
        return idx;
    }

    public void Tick()
    {
        // Resize or recreate buffer if needed. Not we use m_QuadDatas.Length which is high-water mark for quads
        // to avoid constant recreation.
        var requiredInstanceCount = m_QuadDatas.Length;
        if (m_InstanceBuffer == null || requiredInstanceCount != m_QuadCount)
        {
            if (m_InstanceBuffer != null)
                m_InstanceBuffer.Release();

            m_QuadCount = requiredInstanceCount;
            m_InstanceBuffer = new ComputeBuffer(m_QuadCount, 16 + 16 + 16);
            m_DataBuffer = new InstanceData[m_QuadCount];

            instanceMaterialProc.SetBuffer("positionBuffer", m_InstanceBuffer);
            instanceMaterialProc.SetVector("scales", new Vector4(
                1.0f / width,
                1.0f / height,
                (float)cellWidth / instanceMaterialProc.mainTexture.width,
                (float)cellHeight / instanceMaterialProc.mainTexture.height));
        }

        // Scan out quads
        for (int i = 0, c = m_NumQuadsUsed; i < c; i++)
        {
            var r = m_QuadDatas[i];
            var col = r.color;
            var posUV = new Vector4(r.rect.x, r.rect.y, 0, 0);
            if (r.character != '\0')
            {
                posUV.z = (r.character - 32) % charCols;
                posUV.w = (r.character - 32) / charCols;
                col.w = 0.0f;
            }
            m_DataBuffer[i].positionAndUV = posUV;
            m_DataBuffer[i].size = new Vector4(r.rect.z, r.rect.w, 0, 0);
            m_DataBuffer[i].color = col;
        }
        m_InstanceBuffer.SetData(m_DataBuffer, 0, 0, m_NumQuadsUsed);
    }

    void OnPostRender()
    {
        instanceMaterialProc.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumQuadsUsed * 6, 1);

        _Clear();
    }

    void Wrote(float x, float y)
    {
        m_LastWriteX = x;
        m_LastWriteY = y;
    }

    public void DeInit()
    {
        if (m_InstanceBuffer != null)
            m_InstanceBuffer.Release();
        m_InstanceBuffer = null;

        m_DataBuffer = null;
    }

    float m_LastWriteX;
    float m_LastWriteY;
    Color m_CurrentColor = Color.grey;

    struct QuadData
    {
        public Vector4 rect;
        public Vector4 color;
        public char character;
    }

    struct InstanceData
    {
        public Vector4 positionAndUV; // if UV are zero, dont sample
        public Vector4 size; // zw unused
        public Vector4 color;
    }

    int m_NumQuadsUsed = 0;
    QuadData[] m_QuadDatas = new QuadData[0];

    int m_QuadCount = -1;

    ComputeBuffer m_InstanceBuffer;
    InstanceData[] m_DataBuffer;

}
