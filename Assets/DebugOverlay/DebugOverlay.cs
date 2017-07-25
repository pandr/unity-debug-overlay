using UnityEngine;
using System.Collections;

public class DebugOverlay : MonoBehaviour
{
    [Header("Overlay size")]
    [SerializeField]
    int width = 80;
    [SerializeField]
    int height = 25;

    [Header("Font material info")]
    public Material instanceMaterialProc;
    [Tooltip("Number of columns of glyphs on texture")]
    public int charCols = 30;
    [Tooltip("Number of rows of glyphs on texture")]
    public int charRows = 16;
    [Tooltip("Width in pixels of each glyph")]
    public int cellWidth = 32;
    [Tooltip("Height in pixels of each glyph")]
    public int cellHeight = 32;

    public static DebugOverlay instance;

    public static int Width { get { return instance.width; } }
    public static int Height { get { return instance.height; } }

    void Awake()
    {
        m_LineMaterial = new Material(Shader.Find("Instanced/LineShaderProc"));
    }

    public void Init()
    {
        instance = this;
    }

    public void Shutdown()
    {
        if (m_QuadInstanceBuffer != null)
            m_QuadInstanceBuffer.Release();
        m_QuadInstanceBuffer = null;
        m_QuadInstanceData = null;

        if (m_LineInstanceBuffer != null)
            m_LineInstanceBuffer.Release();
        m_LineInstanceBuffer = null;
        m_LineInstanceData = null;

        instance = null;
    }

    public void TickLateUpdate()
    {
        // Recreate compute buffer if needed.
        if (m_QuadInstanceBuffer == null || m_QuadInstanceBuffer.count != m_QuadInstanceData.Length)
        {
            if (m_QuadInstanceBuffer != null)
            {
                m_QuadInstanceBuffer.Release();
                m_QuadInstanceBuffer = null;
            }

            m_QuadInstanceBuffer = new ComputeBuffer(m_QuadInstanceData.Length, 16 + 16 + 16);
            instanceMaterialProc.SetBuffer("positionBuffer", m_QuadInstanceBuffer);
        }

        if (m_LineInstanceBuffer == null || m_LineInstanceBuffer.count != m_LineInstanceData.Length)
        {
            if (m_LineInstanceBuffer != null)
            {
                m_LineInstanceBuffer.Release();
                m_LineInstanceBuffer = null;
            }

            m_LineInstanceBuffer = new ComputeBuffer(m_LineInstanceData.Length, 16 + 16);
            m_LineMaterial.SetBuffer("positionBuffer", m_LineInstanceBuffer);
        }

        m_QuadInstanceBuffer.SetData(m_QuadInstanceData, 0, 0, m_NumQuadsUsed);
        m_NumQuadsToDraw = m_NumQuadsUsed;

        m_LineInstanceBuffer.SetData(m_LineInstanceData, 0, 0, m_NumLinesUsed);
        m_NumLinesToDraw = m_NumLinesUsed;

        instanceMaterialProc.SetVector("scales", new Vector4(
            1.0f / width,
            1.0f / height,
            (float)cellWidth / instanceMaterialProc.mainTexture.width,
            (float)cellHeight / instanceMaterialProc.mainTexture.height));

        m_LineMaterial.SetVector("scales", new Vector4(1.0f / width, 1.0f / height, 1.0f / 1280.0f, 1.0f / 720.0f));

        _Clear();
    }

    /// <summary>
    /// Set color of text. 
    /// </summary>
    /// <param name="col"></param>
    public static void SetColor(Color col)
    {
        if (instance == null)
            return;
        instance.m_CurrentColor = col;
    }

    public static void SetOrigin(float x, float y)
    {
        if (instance == null)
            return;
        instance.m_OriginX = x;
        instance.m_OriginY = y;
    }

    public static void Write(float x, float y, string format)
    {
        if (instance == null)
            return;
        instance._Write(x, y, format, new ArgList0());
    }
    public static void Write<T>(float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        instance._Write(x, y, format, new ArgList1<T>(arg));
    }
    public static void Write<T>(Color col, float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        instance._Write(x, y, format, new ArgList1<T>(arg));
        instance.m_CurrentColor = c;
    }
    public static void Write<T0, T1>(float x, float y, string format, T0 arg0, T1 arg1)
    {
        if (instance == null)
            return;
        instance._Write(x, y, format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public static void Write<T0, T1, T2>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        if (instance == null)
            return;
        instance._Write(x, y, format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
    }

    // Draw a stacked histogram from numSets of data. Data must contain numSets of interleaved, non-negative datapoints.
    public static void DrawHist(float x, float y, float w, float h, float[] data, int startSample, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        instance._DrawHist(x, y, w, h, data, startSample, color, numSets, maxRange);
    }

    static Color[] m_Colors = new Color[1];
    public static void DrawHist(float x, float y, float w, float h, float[] data, int startSample, Color color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        m_Colors[0] = color;
        instance._DrawHist(x, y, w, h, data, startSample, m_Colors, 1, maxRange);
    }

    public static void DrawGraph(float x, float y, float w, float h, float[] data, int startSample, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        instance._DrawGraph(x, y, w, h, data, startSample, color, numSets, maxRange);
    }


    public static void DrawGraph(float x, float y, float w, float h, float[] data, int startSample, Color color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        m_Colors[0] = color;
        instance._DrawGraph(x, y, w, h, data, startSample, m_Colors, 1, maxRange);
    }

    public static void DrawRect(float x, float y, float w, float h, Color col)
    {
        if (instance == null)
            return;
        instance._DrawRect(x, y, w, h, col);
    }

    public static void DrawLine(float x1, float y1, float x2, float y2, Color col)
    {
        if (instance == null)
            return;
        instance.AddLine(x1, y1, x2, y2, col);
    }

    void _DrawText(float x, float y, ref char[] text, int length)
    {
        for (var i = 0; i < length; i++)
        {
            AddQuad(m_OriginX + x + i, m_OriginY + y, 1, 1, text[i], m_CurrentColor);
        }
    }

    void _DrawGraph(float x, float y, float w, float h, float[] data, int startSample, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (data.Length % numSets != 0)
            throw new System.ArgumentException("Length of data must be a multiple of numSets");
        if (color.Length != numSets)
            throw new System.ArgumentException("Length of colors must be numSets");

        var dataLength = data.Length;
        var numSamples = dataLength / numSets;

        float maxData = float.MinValue;

        for (var i = 0; i < dataLength; i++)
        {
            if (data[i] > maxData)
                maxData = data[i];
        }

        if (maxData > maxRange)
            maxRange = maxData;

        float dx = w / numSamples;
        float scale = maxRange > 0 ? h / maxRange : 1.0f;

        for (var j = 0; j < numSets; j++)
        {
            float old_pos_x = 0;
            float old_pos_y = 0;
            Vector4 col = color[j];
            for (var i = 0; i < numSamples; i++)
            {
                float d = data[((i + startSample) % numSamples) * numSets + j];
                var pos_x = m_OriginX + x + dx * i;
                var pos_y = m_OriginY + y + h - d * scale;
                if (i > 0)
                    AddLine(old_pos_x, old_pos_y, pos_x, pos_y, col);
                old_pos_x = pos_x;
                old_pos_y = pos_y;
            }
        }
        AddLine(x, y + h, x + w, y + h, color[0]);
        AddLine(x, y, x, y + h, color[0]);
    }

    void _DrawHist(float x, float y, float w, float h, float[] data, int startSample, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (data.Length % numSets != 0)
            throw new System.ArgumentException("Length of data must be a multiple of numSets");
        if (color.Length != numSets)
            throw new System.ArgumentException("Length of colors must be numSets");

        var dataLength = data.Length;
        var numSamples = dataLength / numSets;

        float maxData = float.MinValue;

        // Find tallest stack of values
        for (var i = 0; i < numSamples; i++)
        {
            float sum = 0;

            for (var j = 0; j < numSets; j++)
                sum += data[i * numSets + j];

            if (sum > maxData)
                maxData = sum;
        }

        if (maxData > maxRange)
            maxRange = maxData;

        float dx = w / numSamples;
        float scale = maxRange > 0 ? h / maxRange : 1.0f;

        float stackOffset = 0;
        for (var i = 0; i < numSamples; i++)
        {
            stackOffset = 0;
            for (var j = 0; j < numSets; j++)
            {
                var c = color[j];
                float d = data[((i + startSample) % numSamples) * numSets + j];
                float barHeight = d * scale; // now in [0, h]
                var pos_x = m_OriginX + x + dx * i;
                var pos_y = m_OriginY + y + h - barHeight - stackOffset;
                var width = dx;
                var height = barHeight;
                stackOffset += barHeight;
                AddQuad(pos_x, pos_y, width, height, '\0', new Vector4(c.r, c.g, c.b, c.a));
            }
        }
    }

    void _DrawRect(float x, float y, float w, float h, Color col)
    {
        AddQuad(m_OriginX + x, m_OriginY + y, w, h, '\0', col);
    }

    void _Clear()
    {
        m_NumQuadsUsed = 0;
        m_NumLinesUsed = 0;
        SetOrigin(0, 0);
    }

    char[] _buf = new char[1024];
    void _Write<T>(float x, float y, string format, T argList) where T : IArgList
    {
        var num = StringFormatter.__Write<T>(ref _buf, 0, format, argList);
        _DrawText(x, y, ref _buf, num);
    }

    void OnPostRender()
    {
        m_LineMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumLinesToDraw * 6, 1);
        instanceMaterialProc.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumQuadsToDraw * 6, 1);
    }

    unsafe void AddLine(float x1, float y1, float x2, float y2, Vector4 col)
    {
        if (m_NumLinesUsed >= m_LineInstanceData.Length)
        {
            // Resize
            var newBuf = new LineInstanceData[m_LineInstanceData.Length + 128];
            System.Array.Copy(m_LineInstanceData, newBuf, m_LineInstanceData.Length);
            m_LineInstanceData = newBuf;
        }
        fixed (LineInstanceData* d = &m_LineInstanceData[m_NumLinesUsed])
        {
            d->color = col;
            d->position.x = x1;
            d->position.y = y1;
            d->position.z = x2;
            d->position.w = y2;
        }
        m_NumLinesUsed++;
    }

    public unsafe void AddQuad(float x, float y, float w, float h, char c, Vector4 col)
    {
        if (m_NumQuadsUsed >= m_QuadInstanceData.Length)
        {
            // Resize
            var newBuf = new QuadInstanceData[m_QuadInstanceData.Length + 128];
            System.Array.Copy(m_QuadInstanceData, newBuf, m_QuadInstanceData.Length);
            m_QuadInstanceData = newBuf;
        }

        fixed (QuadInstanceData* d = &m_QuadInstanceData[m_NumQuadsUsed])
        {
            if (c != '\0')
            {
                d->positionAndUV.z = (c - 32) % charCols;
                d->positionAndUV.w = (c - 32) / charCols;
                col.w = 0.0f;
            }
            else
            {
                d->positionAndUV.z = 0;
                d->positionAndUV.w = 0;
            }

            d->color = col;
            d->positionAndUV.x = x;
            d->positionAndUV.y = y;
            d->size.x = w;
            d->size.y = h;
            d->size.z = 0;
            d->size.w = 0;
        }

        m_NumQuadsUsed++;
    }


    float m_OriginX;
    float m_OriginY;
    Color m_CurrentColor = Color.white;

    struct QuadInstanceData
    {
        public Vector4 positionAndUV; // if UV are zero, dont sample
        public Vector4 size; // zw unused
        public Vector4 color;
    }

    struct LineInstanceData
    {
        public Vector4 position; // segment from (x,y) to (z,w)
        public Vector4 color;
    }

    int m_NumQuadsUsed = 0;
    int m_NumLinesUsed = 0;

    ComputeBuffer m_QuadInstanceBuffer;
    ComputeBuffer m_LineInstanceBuffer;
    int m_NumQuadsToDraw = 0;
    int m_NumLinesToDraw = 0;
    QuadInstanceData[] m_QuadInstanceData = new QuadInstanceData[128];
    LineInstanceData[] m_LineInstanceData = new LineInstanceData[128];

    Material m_LineMaterial;
}
