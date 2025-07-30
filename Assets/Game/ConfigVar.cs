using System;
using UnityEngine;

public static class ConfigVar
{
    // Static arrays for O(1) access - no dictionary lookups in hot loops
    private static string[] m_Names = new string[128];
    private static float[] m_FloatValues = new float[128];
    private static int[] m_IntValues = new int[128];
    private static bool[] m_BoolValues = new bool[128];
    private static ConfigVarType[] m_Types = new ConfigVarType[128];
    private static int m_Count = 0;

    public enum ConfigVarType { Float, Int, Bool }

    // Fast access methods for hot loops
    public static float GetFloat(int id) => m_FloatValues[id];
    public static int GetInt(int id) => m_IntValues[id];
    public static bool GetBool(int id) => m_BoolValues[id];

    // Registration methods
    public static int RegisterFloat(string name, float defaultValue, string description = "")
    {
        int id = m_Count++;
        m_Names[id] = name;
        m_FloatValues[id] = defaultValue;
        m_Types[id] = ConfigVarType.Float;
        
        // Add console command for setting
        Game.console.AddCommand(name, (args) => CmdSetFloat(id, args), description);
        return id;
    }

    public static int RegisterInt(string name, int defaultValue, string description = "")
    {
        int id = m_Count++;
        m_Names[id] = name;
        m_IntValues[id] = defaultValue;
        m_Types[id] = ConfigVarType.Int;
        
        Game.console.AddCommand(name, (args) => CmdSetInt(id, args), description);
        return id;
    }

    public static int RegisterBool(string name, bool defaultValue, string description = "")
    {
        int id = m_Count++;
        m_Names[id] = name;
        m_BoolValues[id] = defaultValue;
        m_Types[id] = ConfigVarType.Bool;
        
        Game.console.AddCommand(name, (args) => CmdSetBool(id, args), description);
        return id;
    }

    // Console command handlers using existing StringFormatter
    private static void CmdSetFloat(int id, string[] args)
    {
        if (args.Length < 1) 
        {
            Game.console.Write("{0} = {1}\n", m_Names[id], m_FloatValues[id]);
            return;
        }
        
        if (float.TryParse(args[0], out float value))
        {
            m_FloatValues[id] = value;
            Game.console.Write("{0} = {1}\n", m_Names[id], value);
        }
        else
        {
            Game.console.Write("Invalid value for {0}\n", m_Names[id]);
        }
    }

    private static void CmdSetInt(int id, string[] args)
    {
        if (args.Length < 1) 
        {
            Game.console.Write("{0} = {1}\n", m_Names[id], m_IntValues[id]);
            return;
        }
        
        if (int.TryParse(args[0], out int value))
        {
            m_IntValues[id] = value;
            Game.console.Write("{0} = {1}\n", m_Names[id], value);
        }
        else
        {
            Game.console.Write("Invalid value for {0}\n", m_Names[id]);
        }
    }

    private static void CmdSetBool(int id, string[] args)
    {
        if (args.Length < 1) 
        {
            Game.console.Write("{0} = {1}\n", m_Names[id], m_BoolValues[id] ? "true" : "false");
            return;
        }
        
        if (bool.TryParse(args[0], out bool value))
        {
            m_BoolValues[id] = value;
            Game.console.Write("{0} = {1}\n", m_Names[id], value ? "true" : "false");
        }
        else
        {
            Game.console.Write("Invalid value for {0}\n", m_Names[id]);
        }
    }

    // List all config vars
    public static void CmdList(string[] args)
    {
        Game.console.Write("Config Variables:\n");
        for (int i = 0; i < m_Count; i++)
        {
            switch (m_Types[i])
            {
                case ConfigVarType.Float:
                    Game.console.Write("  {0} = {1}\n", m_Names[i], m_FloatValues[i]);
                    break;
                case ConfigVarType.Int:
                    Game.console.Write("  {0} = {1}\n", m_Names[i], m_IntValues[i]);
                    break;
                case ConfigVarType.Bool:
                    Game.console.Write("  {0} = {1}\n", m_Names[i], m_BoolValues[i] ? "true" : "false");
                    break;
            }
        }
    }
} 