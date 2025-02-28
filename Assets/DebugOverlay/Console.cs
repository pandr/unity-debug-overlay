using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Console : IGameSystem
{
    const int k_BufferSize = 80 * 25 * 1000; // arbitrarily set to 1000 scroll back lines at 80x25
    const int k_InputBufferSize = 512;
    const int k_HistorySize = 1000; // lines of history

    int m_Width;
    int m_Height;
    int m_NumLines;
    int m_LastLine;
    int m_LastColumn;
    int m_LastVisibleLine;
    float m_ConsoleFoldout;
    float m_ConsoleFoldoutDest;

    bool m_ConsoleOpen;

    char[] m_InputFieldBuffer;
    int m_CursorPos = 0;
    int m_InputFieldLength = 0;

    string[] m_History = new string[k_HistorySize];
    int m_HistoryDisplayIndex = 0;
    int m_HistoryNextIndex = 0;
    List<char> m_InputChars = new List<char>();

    Color m_BackgroundColor = new Color(0, 0, 0, 0.9f);
    Vector4 m_TextColor = new Vector4(0.7f, 1.0f, 0.7f, 1.0f);
    Color m_CursorCol = new Color(0, 0.8f, 0.2f, 0.5f);

    System.UInt32[] m_ConsoleBuffer;

    public Console()
    {
        m_ConsoleBuffer = new System.UInt32[k_BufferSize];
        m_InputFieldBuffer = new char[k_InputBufferSize];
        AddCommand("help", CmdHelp, "Show available commands");
        AddCommand("dump", CmdDumpScene, "Dump scene hierarchy in active scene");
        Keyboard.current.onTextInput += OnTextInput;
    }

    private void CmdDumpScene(string[] args)
    {
        var go = new List<GameObject>();
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(go);
        foreach(var g in go)
        {
            RecurDump(g, 0);
        }
    }

    private void RecurDump(GameObject go, int depth)
    {
        Write("{0}{1}\n", new string(' ', depth), go.name);
        for (var i = 0; i < go.transform.childCount; i++)
        {
            RecurDump(go.transform.GetChild(i).gameObject, depth + 1);
        }
    }

    private void OnTextInput(char c)
    {
        if (!m_ConsoleOpen)
            return;

        if(Char.IsControl(c) && c != '\n' && c != '\r' && c != '\b')
        {
            return;
        }

        m_InputChars.Add(c);
    }

    void CmdHelp(string[] args)
    {
        foreach (var c in m_Commands)
        {
            Write("  {0,-15} {1}\n", c.Key, m_CommandDescriptions[c.Key]);
        }
    }

    public void Init()
    {
        Init(null);
    }

    public void Init(DebugOverlay debugOverlay)
    {
        m_DebugOverlay = debugOverlay != null ? debugOverlay : DebugOverlay.instance;
        Resize(m_DebugOverlay.width, m_DebugOverlay.height);
        Clear();
    }

    public void Shutdown()
    {
    }

    public delegate void CommandDelegate(string[] args);
    Dictionary<string, CommandDelegate> m_Commands = new Dictionary<string, CommandDelegate>();
    Dictionary<string, string> m_CommandDescriptions = new Dictionary<string, string>();

    public void AddCommand(string name, CommandDelegate callback, string description)
    {
        if (m_Commands.ContainsKey(name))
        {
            Write("Cannot add command {0} twice", name);
            return;
        }
        m_Commands.Add(name, callback);
        m_CommandDescriptions.Add(name, description);
    }

    public void Resize(int width, int height)
    {
        m_Width = width;
        m_Height = height;
        m_NumLines = m_ConsoleBuffer.Length / m_Width;

        m_LastLine = m_Height - 1;
        m_LastVisibleLine = m_Height - 1;
        m_LastColumn = 0;

        // TODO: copy old text to resized console
    }

    public void Clear()
    {
        for (int i = 0, c = m_ConsoleBuffer.Length; i < c; i++)
            m_ConsoleBuffer[i] = 0;
        m_LastColumn = 0;
    }

    public void Show(float shown)
    {
        m_ConsoleFoldoutDest = shown;
    }

    public void TickUpdate()
    {
        if(Keyboard.current.f12Key.wasPressedThisFrame)
        {
            m_ConsoleOpen = !m_ConsoleOpen;
            m_ConsoleFoldoutDest = m_ConsoleOpen ? 1.0f : 0.0f;
            Show(m_ConsoleFoldoutDest);
        }

        if (!m_ConsoleOpen)
            return;

        Scroll((int)Mouse.current.scroll.ReadValue().y);

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame && m_CursorPos > 0)
            m_CursorPos--;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame && m_CursorPos < m_InputFieldLength)
            m_CursorPos++;
        else if (Keyboard.current.homeKey.wasPressedThisFrame || (Keyboard.current.aKey.wasPressedThisFrame && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)))
            m_CursorPos = 0;
        else if (Keyboard.current.endKey.wasPressedThisFrame || (Keyboard.current.eKey.wasPressedThisFrame && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)))
            m_CursorPos = m_InputFieldLength;
        else if (Keyboard.current.tabKey.wasPressedThisFrame)
            TabComplete();
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            HistoryPrev();
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            HistoryNext();
        else
        {
            for (var i = 0; i < m_InputChars.Count; i++)
            {
                var ch = m_InputChars[i];
                if (ch == '\b')
                    Backspace();
                else if (ch == '\n' || ch == '\r')
                {
                    var s = new string(m_InputFieldBuffer, 0, m_InputFieldLength);
                    HistoryStore(s);
                    ExecuteCommand(s);
                    m_InputFieldLength = 0;
                    m_CursorPos = 0;
                }
                else
                    Type(ch);
            }
            m_InputChars.Clear();
        }
    }

    void HistoryPrev()
    {
        if (m_HistoryDisplayIndex == 0 || m_HistoryNextIndex - m_HistoryDisplayIndex >= m_History.Length - 1)
            return;

        if (m_HistoryDisplayIndex == m_HistoryNextIndex)
            m_History[m_HistoryNextIndex % m_History.Length] = new string(m_InputFieldBuffer, 0, m_InputFieldLength);

        m_HistoryDisplayIndex--;

        var s = m_History[m_HistoryDisplayIndex % m_History.Length];

        s.CopyTo(0, m_InputFieldBuffer, 0, s.Length);
        m_InputFieldLength = s.Length;
        m_CursorPos = s.Length;
    }

    void HistoryNext()
    {
        if (m_HistoryDisplayIndex == m_HistoryNextIndex)
            return;


        m_HistoryDisplayIndex++;

        var s = m_History[m_HistoryDisplayIndex % m_History.Length];

        s.CopyTo(0, m_InputFieldBuffer, 0, s.Length);
        m_InputFieldLength = s.Length;
        m_CursorPos = s.Length;
    }

    void HistoryStore(string cmd)
    {
        m_History[m_HistoryNextIndex % m_History.Length] = cmd;
        m_HistoryNextIndex++;
        m_HistoryDisplayIndex = m_HistoryNextIndex;
    }

    void ExecuteCommand(string command)
    {
        var splitCommand = command.Split(null as char[], System.StringSplitOptions.RemoveEmptyEntries);
        if (splitCommand.Length < 1)
            return;

        Write('>' + string.Join(" ", splitCommand) + '\n');
        var commandName = splitCommand[0].ToLower();

        CommandDelegate commandDelegate;

        if (m_Commands.TryGetValue(commandName, out commandDelegate))
        {
            var arguments = new string[splitCommand.Length - 1];
            System.Array.Copy(splitCommand, 1, arguments, 0, splitCommand.Length - 1);
            commandDelegate(arguments);
        }
        else
        {
            Write("Unknown command: {0}\n", splitCommand[0]);
        }
    }

    public void TickLateUpdate()
    {
        if (m_ConsoleFoldout < m_ConsoleFoldoutDest)
        {
            m_ConsoleFoldout = Mathf.Min(m_ConsoleFoldoutDest, m_ConsoleFoldout + Time.deltaTime * 5.0f);
        }
        else if (m_ConsoleFoldout > m_ConsoleFoldoutDest)
        {
            m_ConsoleFoldout = Mathf.Max(m_ConsoleFoldoutDest, m_ConsoleFoldout - Time.deltaTime * 5.0f);
        }

        if (m_ConsoleFoldout <= 0.0f)
            return;

        var yoffset = -(float)m_Height * (1.0f - m_ConsoleFoldout);
        m_DebugOverlay._DrawRect(0, 0 + yoffset, m_Width, m_Height, m_BackgroundColor);

        var line = m_LastVisibleLine;
        if ((m_LastVisibleLine == m_LastLine) && (m_LastColumn == 0))
        {
            line -= 1;
        }

        Vector4 col = m_TextColor;
        UInt32 icol = 0;
        for (var i = 0; i < m_Height - 1; i++, line--)
        {
            var idx = (line % m_NumLines) * m_Width;
            for (var j = 0; j < m_Width; j++)
            {
                UInt32 c = m_ConsoleBuffer[idx + j];
                char ch = (char)(c & 0xff);
                if (icol != (c & 0xffffff00))
                {
                    icol = c & 0xffffff00;
                    col.x = (float)((icol >> 24) & 0xff) / 255.0f;
                    col.y = (float)((icol >> 16) & 0xff) / 255.0f;
                    col.z = (float)((icol >> 8) & 0xff) / 255.0f;
                }
                if (c != '\0')
                    m_DebugOverlay.AddQuad(j, m_Height - 2 - i + yoffset, 1, 1, ch, col);
            }
        }

        // Draw input line
        var horizontalScroll = m_CursorPos - m_Width + 1;
        horizontalScroll = Mathf.Max(0, horizontalScroll);
        for (var i = horizontalScroll; i < m_InputFieldLength; i++)
        {
            char c = m_InputFieldBuffer[i];
            if (c != '\0')
                m_DebugOverlay.AddQuad(i - horizontalScroll, m_Height - 1 + yoffset, 1, 1, c, m_TextColor);
        }
        m_DebugOverlay.AddQuad(m_CursorPos - horizontalScroll, m_Height - 1 + yoffset, 1, 1, '\0', m_CursorCol);
    }

    void NewLine()
    {
        // Only scroll view if at bottom
        if (m_LastVisibleLine == m_LastLine)
            m_LastVisibleLine++;

        m_LastLine++;
        m_LastColumn = 0;
    }

    void Scroll(int amount)
    {
        m_LastVisibleLine += amount;

        // Prevent going past last line
        if (m_LastVisibleLine > m_LastLine)
            m_LastVisibleLine = m_LastLine;

        if (m_LastVisibleLine < m_Height - 1)
            m_LastVisibleLine = m_Height - 1;

        // Prevent wrapping around
        if (m_LastVisibleLine < m_LastLine - m_NumLines + m_Height)
            m_LastVisibleLine = m_LastLine - m_NumLines + m_Height;
    }

    public void _Write(char[] buf, int length)
    {
        const string hexes = "0123456789ABCDEF";
        UInt32 col = 0xBBBBBB00;
        for (int i = 0; i < length; i++)
        {
            if (buf[i] == '\n')
            {
                NewLine();
                continue;
            }
            // Parse color markup of the form ^AF7 -> color(0xAA, 0xFF, 0x77)
            if (buf[i] == '^' && i < length - 3)
            {
                UInt32 res = 0;
                for (var j = i + 1; j < i + 4; j++)
                {
                    var v = (uint)hexes.IndexOf(buf[j]);
                    res = res * 256 + v * 16 + v;
                }
                col = res << 8;
                i += 3;
                continue;
            }
            var idx = (m_LastLine % m_NumLines) * m_Width + m_LastColumn;
            m_ConsoleBuffer[idx] = col | (byte)buf[i];
            m_LastColumn++;
            if (m_LastColumn >= m_Width)
            {
                NewLine();
            }
        }
    }

    static char[] _buf = new char[1024];

    public void Write(string format)
    {
        var l = StringFormatter.Write(ref _buf, 0, format);
        _Write(_buf, l);
    }

    public void Write<T>(string format, T arg)
    {
        var l = StringFormatter.Write(ref _buf, 0, format, arg);
        _Write(_buf, l);
    }
    public void Write<T0, T1>(string format, T0 arg0, T1 arg1)
    {
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
        _Write(_buf, l);
    }
    public void Write<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    {
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
        _Write(_buf, l);
    }

    void Type(char c)
    {
        if (m_InputFieldLength >= m_InputFieldBuffer.Length)
            return;

        System.Array.Copy(m_InputFieldBuffer, m_CursorPos, m_InputFieldBuffer, m_CursorPos + 1, m_InputFieldLength - m_CursorPos);
        m_InputFieldBuffer[m_CursorPos] = c;
        m_CursorPos++;
        m_InputFieldLength++;
    }

    void Backspace()
    {
        if (m_CursorPos == 0)
            return;
        System.Array.Copy(m_InputFieldBuffer, m_CursorPos, m_InputFieldBuffer, m_CursorPos - 1, m_InputFieldLength - m_CursorPos);
        m_CursorPos--;
        m_InputFieldLength--;
    }

    void TabComplete()
    {
        string prefix = new string(m_InputFieldBuffer, 0, m_CursorPos);

        // Look for possible tab completions
        List<string> matches = new List<string>();

        foreach (var c in m_Commands)
        {
            var name = c.Key;
            if (!name.StartsWith(prefix, true, null))
                continue;
            matches.Add(name);
        }

        if (matches.Count == 0)
            return;

        // Look for longest common prefix
        int lcp = matches[0].Length;
        for (var i = 0; i < matches.Count - 1; i++)
        {
            lcp = Mathf.Min(lcp, CommonPrefix(matches[i], matches[i + 1]));
        }
        var bestMatch = matches[0].Substring(prefix.Length, lcp - prefix.Length);
        foreach (var c in bestMatch)
            Type(c);
        if (matches.Count > 1)
        {
            // write list of possible completions
            for (var i = 0; i < matches.Count; i++)
                Write(" {0}\n", matches[i]);
        }

        if (matches.Count == 1)
            Type(' ');
    }

    // Returns length of largest common prefix of two strings
    static int CommonPrefix(string a, string b)
    {
        int minl = Mathf.Min(a.Length, b.Length);
        for (int i = 1; i <= minl; i++)
        {
            if (!a.StartsWith(b.Substring(0, i), true, null))
                return i - 1;
        }
        return minl;
    }

    DebugOverlay m_DebugOverlay;
}
