using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// The Game object before anything else (through BootSequence) and it is a global.
/// This is where all game systems are constructed and initialized and ticked from.
/// </summary>

interface IGameSystem
{
    void Init();
    void Shutdown();
    void TickUpdate();
}

public class Game
{
    private static Game _instance;

    static public Console console { get { return _instance.m_Console; } }
    static public DebugOverlay debugOverlay { get { return _instance.m_DebugOverlay; } }

    DebugOverlay m_DebugOverlay;
    Console m_Console;
    Stats m_Stats;

    public void Init()
    {
        Debug.Assert(_instance == null);
        _instance = this;

        m_DebugOverlay = new DebugOverlay();
        m_DebugOverlay.Init(120, 36);

        m_Console = new Console();
        m_Console.Init();

        m_Console.AddCommand("quit", CmdQuit, "Quit game");

        m_Stats = new Stats();
        m_Stats.Init();

        Game.console.Write("^FFFGame initialized^F44.^4F4.^44F.\n");
    }

    public void Shutdown()
    {
        Debug.Assert(_instance == this);

        m_DebugOverlay.Shutdown();
        m_DebugOverlay = null;
        m_Console.Shutdown();
        m_Console = null;
        m_Stats.Shutdown();
        m_Stats = null;

        _instance = null;
    }

    void CmdQuit(string[] args)
    {
        Game.console.Write("Goodbye\n");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Update()
    {
        m_Stats.TickUpdate();
        m_Console.TickUpdate();
    }


    public void LateUpdate()
    {
        m_Console.TickLateUpdate();
        m_DebugOverlay.TickLateUpdate();
    }

    public void FixedUpdate()
    {
    }

    public void EndOfFrame()
    {
        m_DebugOverlay.Render();
    }
}
