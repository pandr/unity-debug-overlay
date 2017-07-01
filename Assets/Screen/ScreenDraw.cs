using UnityEngine;
using System.Collections;

public class ScreenDraw : MonoBehaviour
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
    
    public void Init()
    {
        var mesh = new Mesh();
        var vertices = new Vector3[4];
        var triangles = new int[6];
        var uvs = new Vector2[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(1, 0, 0);
        vertices[2] = new Vector3(1, 1, 0);
        vertices[3] = new Vector3(0, 1, 0);
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        m_InstanceMesh = mesh;

        m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        m_Screen = new Screen(width, height);
    }

    float[] fpsArray = new float[100];
    float[] moveArray = new float[100];
    void Test()
    {
        m_Screen.Write(2, 2, "Hello, {0,-5} world!", Time.frameCount%100<50 ? "Happy" : "Evil");
        m_Screen.Write(2, 3, "FrameNo: {0,7}", Time.frameCount);
        m_Screen.Write(2, 4, "Time:    {0,7:0000.00}", Time.time);
        m_Screen.Write(2, 5, "FPS:     {0,7:###.##}", 1.0f / Time.deltaTime);
        m_Screen.DrawRect(20, 5.2f, 1.0f/Time.deltaTime*0.1f, 0.6f, Color.green);
        m_Screen.Write(2, 6, "PlayerPos: {0,6:000.0} {1,6:000.0} {2,6:000.0}",
            transform.position.x,
            transform.position.y,
            transform.position.z);

        /// Graphing
        System.Array.Copy(fpsArray, 1, fpsArray, 0, fpsArray.Length - 1);
        fpsArray[fpsArray.Length - 1] = 1.0f / Time.deltaTime;
        m_Screen.DrawHist(20, 10, 20, 3, fpsArray, Color.blue, 120.0f);

        System.Array.Copy(moveArray, 1, moveArray, 0, moveArray.Length - 1);
        moveArray[moveArray.Length - 1] = transform.parent.position.y;
        m_Screen.DrawHist(20, 15, 20, 3, moveArray, Color.red, 4.0f);

        m_Screen.Write(74, 24, "Hey {0}", -1);
    }

    struct InstanceData
    {
        public Vector4 positionAndUV; // if UV are zero, dont sample
        public Vector4 size; // zw unused
        public Vector4 color;
    }

    public void Tick()
    {
        // Test code

        Test();

        // Actual update code

        // Resize or recreate buffer if needed
        var requiredInstanceCount = m_Screen.width * m_Screen.height + m_Screen.extras.Length;
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
        var charBuffer = m_Screen.charBuffer;
        int screenWidth = m_Screen.width;
        var v4 = new Vector4();
        var size = new Vector4(1, 1, 0, 0);
        var color = new Color(2, 1.0f,0 , 0.0f);
        for (int i = 0, c = charBuffer.Length; i < c; i++)
        {
            var ch = charBuffer[i];
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
        for(int i = 0, c = m_Screen.numExtras; i<c; i++)
        {
            var r = m_Screen.extras[i];
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

        // Draw
        Graphics.DrawMeshInstancedIndirect(m_InstanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), m_ArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, cam);

        // Resize screen for next frame if needed
        if (m_Screen.width != width || m_Screen.height != height)
            m_Screen.Resize(width, height);

        m_Screen.Clear();
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

    Screen m_Screen;

    int m_InstanceCount = -1;
    Mesh m_InstanceMesh;

    ComputeBuffer m_InstanceBuffer;
    InstanceData[] m_DataBuffer;
    ComputeBuffer m_ArgsBuffer;
    uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };

}
