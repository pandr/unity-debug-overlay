using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Console
{
    int m_Width = 80;
    int m_Height = 25;
    int m_NumLines;
    int m_LastLine;
    int m_LastColumn;
    int m_LastVisibleLine;
    float m_ConsoleFoldout;
    float m_ConsoleFoldoutDest;

    enum ConsoleState
    {
        Closed,
        SneakPeek,
        Open
    }
    ConsoleState m_ConsoleState;

    char[] m_InputFieldBuffer;
    int m_CursorPos = 0;
    int m_InputFieldLength = 0;

    Color m_BackgroundColor = new Color(0, 0, 0, 0.9f);
    Vector4 m_TextColor = new Vector4(0.7f, 1.0f, 0.7f, 1.0f);
    Color m_CursorCol = new Color(0, 0.8f, 0.2f, 0.5f);

    const int k_BufferSize = 80 * 25 * 1000; // arbitrarily set to 1000 scroll back lines at 80x25
    const int k_InputBufferSize = 512;

    System.UInt32[] m_ConsoleBuffer;

    public Console()
    {
        m_ConsoleBuffer = new System.UInt32[k_BufferSize];
        m_InputFieldBuffer = new char[k_InputBufferSize];
        AddCommand("help", CmdHelp, "Show available commands");
    }

    void CmdHelp(string[] args)
    {
        foreach (var c in m_Commands)
        {
            Write("  {0,-15} {1}\n", c.Key, m_CommandDescriptions[c.Key]);
        }
    }

    public void Init(int width, int height)
    {
        Resize(width, height);
        Clear();
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
        var oldWidth = m_Width;
        var oldHeight = m_Height;

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
        if (Input.GetKeyDown(KeyCode.F12))
        {
            switch (m_ConsoleState)
            {
                case ConsoleState.Closed:
                    m_ConsoleState = ConsoleState.Open;
                    m_ConsoleFoldoutDest = 1.0f;
                    break;
                case ConsoleState.Open:
                    m_ConsoleState = ConsoleState.SneakPeek;
                    m_ConsoleFoldoutDest = 0.1f;
                    break;
                case ConsoleState.SneakPeek:
                    m_ConsoleState = ConsoleState.Closed;
                    m_ConsoleFoldoutDest = 0.0f;
                    break;
            }
            Show(m_ConsoleFoldoutDest);
        }

        if (m_ConsoleState != ConsoleState.Open)
            return;

        Scroll((int)Input.mouseScrollDelta.y);
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) && m_CursorPos > 0)
                m_CursorPos--;
            else if (Input.GetKeyDown(KeyCode.RightArrow) && m_CursorPos < m_InputFieldLength)
                m_CursorPos++;
            else if (Input.GetKeyDown(KeyCode.Home) || (Input.GetKeyDown(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
                m_CursorPos = 0;
            else if (Input.GetKeyDown(KeyCode.End) || (Input.GetKeyDown(KeyCode.E) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
                m_CursorPos = m_InputFieldLength;
            else if (Input.GetKeyDown(KeyCode.Backspace))
                Backspace();
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteCommand(new string(m_InputFieldBuffer, 0, m_InputFieldLength));
                m_InputFieldLength = 0;
                m_CursorPos = 0;
            }
            else
            {
                // TODO replace with garbage free alternative (perhaps impossible until new input system)
                var ch = Input.inputString;
                for (var i = 0; i < ch.Length; i++)
                    Type(ch[i]);
            }
        }
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
        DebugOverlay.DrawRect(0, 0 + yoffset, m_Width, m_Height, m_BackgroundColor);

        var line = m_LastVisibleLine;
        if ((m_LastVisibleLine == m_LastLine) && (m_LastColumn == 0))
        {
            line -= 1;
        }

        for (var i = 0; i < m_Height - 1; i++, line--)
        {
            var idx = (line % m_NumLines) * m_Width;
            for (var j = 0; j < m_Width; j++)
            {
                char c = (char)(m_ConsoleBuffer[idx + j] & 0xff);
                if (c != '\0')
                    DebugOverlay.instance.AddQuad(j, m_Height - 2 - i + yoffset, 1, 1, c, m_TextColor);
            }
        }

        // Draw input line
        var horizontalScroll = m_CursorPos - m_Width + 1;
        horizontalScroll = Mathf.Max(0, horizontalScroll);
        for (var i = horizontalScroll; i < m_InputFieldLength; i++)
        {
            char c = m_InputFieldBuffer[i];
            if (c != '\0')
                DebugOverlay.instance.AddQuad(i - horizontalScroll, m_Height - 1 + yoffset, 1, 1, c, m_TextColor);
        }
        DebugOverlay.instance.AddQuad(m_CursorPos - horizontalScroll, m_Height - 1 + yoffset, 1, 1, '\0', m_CursorCol);
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
        for (int i = 0; i < length; i++)
        {
            if (buf[i] == '\n')
            {
                NewLine();
                continue;
            }
            var idx = (m_LastLine % m_NumLines) * m_Width + m_LastColumn;
            m_ConsoleBuffer[idx] = (byte)buf[i];
            m_LastColumn++;
            if (m_LastColumn >= m_Width)
            {
                NewLine();
            }
        }
    }

    char[] _buf = new char[1024];
    public void _Write<T>(string format, T argList) where T : IArgList
    {
        var length = StringFormatter.__Write<T>(ref _buf, 0, format, argList);
        _Write(_buf, length);
    }

    public void Write(string format)
    {
        _Write(format, new ArgList0());
    }
    public void Write<T>(string format, T arg)
    {
        _Write(format, new ArgList1<T>(arg));
    }
    public void Write<T0, T1>(string format, T0 arg0, T1 arg1)
    {
        _Write(format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public void Write<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    {
        _Write(format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
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
}