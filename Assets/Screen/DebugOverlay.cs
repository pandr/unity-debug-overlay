using UnityEngine;
using System.Collections;

public class DebugOverlay : MonoBehaviour
{
    public Camera cam;

    [Header("Overlay size")]
    public int width = 80;
    public int height = 25;

    [Header("Font material info")]
    public Material instanceMaterial;
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

        m_InstanceMesh = new Mesh();

        m_InstanceMesh.vertices = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
        };
        m_InstanceMesh.triangles = new int[] {
            0,1,2,
            0,2,3
        };
        m_InstanceMesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        Resize(width, height);
    }

    public static void Write(int x, int y, string format)
    {
        instance._Write(x, y, format, new ArgList0());
    }
    public static void Write<T>(int x, int y, string format, T arg)
    {
        instance._Write(x, y, format, new ArgList1<T>(arg));
    }
    public static void Write<T0, T1>(int x, int y, string format, T0 arg0, T1 arg1)
    {
        instance._Write(x, y, format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public static void Write<T0, T1, T2>(int x, int y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        instance._Write(x, y, format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
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

    void _DrawHist(float x, float y, float w, float h, float[] data, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (data.Length % numSets != 0)
            throw new System.ArgumentException("Length of data must be a multiple of numSets");
        if(color.Length != numSets)
            throw new System.ArgumentException("Length of colors must be numSets");

        var numSamples = data.Length / numSets;
        var idx = AllocExtras(numSets * numSamples);

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

        ExtraData extraData;
        extraData.character = '\0';
        for (var i = 0; i < numSamples; i++)
        {
            float stackOffset = 0;
            for (var j = 0; j < numSets; j++)
            {
                extraData.color = color[j];
                float d = data[i * numSets + j];
                float scaledData = d * scale; // now in [0, h]
                extraData.rect = new Vector4(x + dx * i, y + h - d * scale - stackOffset, dx, d * scale);
                stackOffset += scaledData;
                m_Extras[idx + i * numSets + j] = extraData;
            }
        }
    }

    void _DrawRect(float x, float y, float w, float h, Color col)
    {
        ExtraData rd;
        rd.rect = new Vector4(x, y, w, h);
        rd.color = col;
        var idx = AllocExtras(1);
        rd.character = '\0';
        m_Extras[idx] = rd;
    }

    void Resize(int width, int height)
    {
        m_CharBuffer = new char[width * height];
    }

    void _Clear()
    {
        for (int i = 0, c = m_CharBuffer.Length; i < c; i++)
            m_CharBuffer[i] = (char)0;
        m_NumExtrasUsed = 0;
    }

    void _Write<T>(int x, int y, string format, T argList) where T : IArgList
    {
        int i = width * y + x;
        if (i < 0 || i >= m_CharBuffer.Length)
            return;
        StringFormatter.__Write<T>(ref m_CharBuffer, i, format, argList);
    }

    int AllocExtras(int num)
    {
        var idx = m_NumExtrasUsed;
        m_NumExtrasUsed += num;
        if (m_NumExtrasUsed > m_Extras.Length)
        {
            var newRects = new ExtraData[m_NumExtrasUsed + 128];
            System.Array.Copy(m_Extras, newRects, m_Extras.Length);
            m_Extras = newRects;
        }
        return idx;
    }

    public void Tick()
    {
        // Resize or recreate buffer if needed. Not we use extras.Length which is high-water mark for extras
        // to avoid constant recreation.
        var requiredInstanceCount = width * height + m_Extras.Length;
        if (m_InstanceBuffer == null || requiredInstanceCount != m_InstanceCount)
        {
            if (m_InstanceBuffer != null)
                m_InstanceBuffer.Release();

            m_InstanceCount = requiredInstanceCount;
            m_InstanceBuffer = new ComputeBuffer(m_InstanceCount, 16+16+16);
            m_DataBuffer = new InstanceData[m_InstanceCount];

            instanceMaterial.SetBuffer("positionBuffer", m_InstanceBuffer);
            instanceMaterial.SetFloat("sdx", 1.0f / width);
            instanceMaterial.SetFloat("sdy", 1.0f / height);
            instanceMaterial.SetFloat("tdx", (float)cellWidth / instanceMaterial.mainTexture.width);
            instanceMaterial.SetFloat("tdy", (float)cellHeight / instanceMaterial.mainTexture.height);
        }

        // Scan out content of screen buffer
        int usedInstances = 0;
        int screenWidth = width;
        var v4 = new Vector4();
        var size = new Vector4(1, 1, 0, 0);
        var color = new Color(2, 1.0f,0 , 0.0f);
        for (int i = 0, c = m_CharBuffer.Length; i < c; i++)
        {
            var ch = m_CharBuffer[i];
            if (ch == 0)
                continue;
            v4.x = i % screenWidth; // pos
            v4.y = i / screenWidth; // pos
            v4.z = (ch - 32) % charCols; // uv
            v4.w = (ch - 32) / charCols; // uv
            m_DataBuffer[usedInstances].positionAndUV = v4;
            m_DataBuffer[usedInstances].size = size;
            m_DataBuffer[usedInstances].color = color;
            usedInstances++;
        }

        // Scan out extras
        for(int i = 0, c = m_NumExtrasUsed; i<c; i++)
        {
            var r = m_Extras[i];
            var posUV = new Vector4(r.rect.x, r.rect.y, 0, 0);
            if(r.character != '\0')
            {
                posUV.z = (r.character - 32) % charCols;
                posUV.w = (r.character-32) / charCols;
            }
            m_DataBuffer[usedInstances].positionAndUV = posUV;
            m_DataBuffer[usedInstances].size = new Vector4(r.rect.z, r.rect.w, 0, 0);
            m_DataBuffer[usedInstances].color = r.color;
            usedInstances++;
        }
        m_InstanceBuffer.SetData(m_DataBuffer, 0, 0, usedInstances);

        // Update indirect args
        uint numIndices = (m_InstanceMesh != null) ? (uint)m_InstanceMesh.GetIndexCount(0) : 0;
        m_Args[0] = numIndices;
        m_Args[1] = (uint)usedInstances;
        m_ArgsBuffer.SetData(m_Args);

        // Issue draw
        Graphics.DrawMeshInstancedIndirect(m_InstanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), m_ArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, cam);

        if(width*height != m_CharBuffer.Length)
            Resize(width, height);

        _Clear();
    }

    public void DeInit()
    {
        if (m_InstanceBuffer != null)
            m_InstanceBuffer.Release();
        m_InstanceBuffer = null;

        if (m_ArgsBuffer != null)
            m_ArgsBuffer.Release();
        m_ArgsBuffer = null;

        m_DataBuffer = null;
    }

    char[] m_CharBuffer;

    struct ExtraData
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

    int m_NumExtrasUsed = 0;
    ExtraData[] m_Extras = new ExtraData[0];

    int m_InstanceCount = -1;
    Mesh m_InstanceMesh;

    ComputeBuffer m_InstanceBuffer;
    InstanceData[] m_DataBuffer;
    ComputeBuffer m_ArgsBuffer;
    uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };

}
