using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Base interface for type-erased cvar access
/// </summary>
public interface ICVar
{
    string Name { get; }
    string Description { get; }
    object Value { get; }
    object DefaultValue { get; }
    bool TrySetFromString(string value);
    void Reset();
    string ToString();
}

/// <summary>
/// Make CVar<T> implement ICVar for type-erased access
/// </summary>
public partial class CVar<T> : ICVar where T : IComparable<T>, IConvertible
{
    object ICVar.Value => _value;
    object ICVar.DefaultValue => _defaultValue;
}

/// <summary>
/// System that manages all config variables and integrates with console
/// </summary>
public static class CVarSystem
{
    private static readonly Dictionary<string, ICVar> s_CVars = new Dictionary<string, ICVar>();
    private static bool s_Initialized = false;

    /// <summary>
    /// Initialize the cvar system (called by Game.Init)
    /// </summary>
    public static void Initialize(Console console)
    {
        if (s_Initialized)
            return;

        s_Initialized = true;
        
        // Register console commands
        console.AddCommand("cvarlist", CmdCVarList, "List all config variables");
        console.AddCommand("cvarreset", CmdCVarReset, "Reset a cvar to default value");
        console.AddCommand("cvarresetall", CmdCVarResetAll, "Reset all cvars to default values");
        console.AddCommand("set", CmdSet, "Set a cvar value (set <name> <value>)");

        // Add save/load commands
        console.AddCommand("cvarsave", CmdCVarSave, "Save all cvars to config file");
        console.AddCommand("cvarload", CmdCVarLoad, "Load cvars from config file");
        console.AddCommand("cvarexec", CmdCVarExec, "Execute a config file");
        
        Debug.Log($"CVarSystem initialized with {s_CVars.Count} variables");
    }

    /// <summary>
    /// Register a cvar (called automatically by CVar constructor)
    /// </summary>
    public static void Register(ICVar cvar)
    {
        if (s_CVars.ContainsKey(cvar.Name))
        {
            Debug.LogWarning($"CVar '{cvar.Name}' is already registered!");
            return;
        }

        s_CVars[cvar.Name] = cvar;
        
        if (s_Initialized)
            Debug.Log($"Registered cvar: {cvar.Name} = {cvar.Value}");
    }

    /// <summary>
    /// Get a cvar by name
    /// </summary>
    public static ICVar GetCVar(string name)
    {
        s_CVars.TryGetValue(name.ToLower(), out ICVar cvar);
        return cvar;
    }

    /// <summary>
    /// Get all registered cvars
    /// </summary>
    public static IEnumerable<ICVar> GetAllCVars()
    {
        return s_CVars.Values;
    }

    /// <summary>
    /// Try to set a cvar value from string
    /// </summary>
    public static bool TrySetCVar(string name, string value)
    {
        var cvar = GetCVar(name);
        if (cvar == null)
            return false;

        return cvar.TrySetFromString(value);
    }

    /// <summary>
    /// Handle console input that might be a cvar assignment (name = value)
    /// Returns true if handled as cvar assignment
    /// </summary>
    public static bool TryHandleConsoleInput(string input, Console console)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        // Handle "name = value" syntax
        if (input.Contains("="))
        {
            var parts = input.Split('=');
            if (parts.Length == 2)
            {
                string name = parts[0].Trim();
                string value = parts[1].Trim();
                
                var cvar = GetCVar(name);
                if (cvar != null)
                {
                    if (cvar.TrySetFromString(value))
                    {
                        console.Write("^0F0{0}^FFF = {1}\n", name, cvar.ToString());
                        return true;
                    }
                    else
                    {
                        console.Write("^F00Invalid value '{0}' for {1}\n", value, name);
                        return true;
                    }
                }
            }
        }

        // Handle just "name" to show current value
        var showCvar = GetCVar(input);
        if (showCvar != null)
        {
            console.Write("^0F0{0}^FFF = {1} (default: {2})\n", 
                         showCvar.Name, showCvar.ToString(), showCvar.DefaultValue);
            console.Write("  {0}\n", showCvar.Description);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Save all cvar values to a config file
    /// </summary>
    public static void SaveConfig(string filePath = null)
    {
        if (string.IsNullOrEmpty(filePath))
            filePath = Path.Combine(Application.persistentDataPath, "config.cfg");

        try
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("// Generated config file");
                writer.WriteLine("// Format: <name> = <value>");
                writer.WriteLine();

                foreach (var cvar in s_CVars.Values)
                {
                    writer.WriteLine($"{cvar.Name} = {cvar.ToString()}");
                }
            }
            
            Debug.Log($"Saved {s_CVars.Count} cvars to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save config: {e.Message}");
        }
    }

    /// <summary>
    /// Load cvar values from a config file
    /// </summary>
    public static void LoadConfig(string filePath = null)
    {
        if (string.IsNullOrEmpty(filePath))
            filePath = Path.Combine(Application.persistentDataPath, "config.cfg");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Config file not found: {filePath}");
            return;
        }

        try
        {
            int loadedCount = 0;
            string[] lines = File.ReadAllLines(filePath);
            
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                    continue;
                
                // Parse cvar assignment (without console output during load)
                if (trimmed.Contains("="))
                {
                    var parts = trimmed.Split('=');
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string value = parts[1].Trim();
                        
                        if (TrySetCVar(name, value))
                            loadedCount++;
                    }
                }
            }
            
            Debug.Log($"Loaded {loadedCount} cvars from: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load config: {e.Message}");
        }
    }

    // Console command implementations
    private static void CmdCVarList(string[] args)
    {
        var console = Game.console;
        console.Write("^FF0Config Variables:\n");
        
        foreach (var cvar in s_CVars.Values)
        {
            console.Write("  ^0F0{0,-20}^FFF = {1,-15} ^888(default: {2})\n", 
                         cvar.Name, cvar.ToString(), cvar.DefaultValue.ToString());
            if (!string.IsNullOrEmpty(cvar.Description))
                console.Write("    ^AAA{0}\n", cvar.Description);
        }
        
        console.Write("^FF0Total: {0} variables\n", s_CVars.Count);
        console.Write("^888Usage: <name> = <value> or just <name> to show current value\n");
    }

    private static void CmdCVarReset(string[] args)
    {
        var console = Game.console;
        
        if (args.Length != 1)
        {
            console.Write("Usage: cvarreset <name>\n");
            return;
        }

        var cvar = GetCVar(args[0]);
        if (cvar == null)
        {
            console.Write("^F00Unknown cvar: {0}\n", args[0]);
            return;
        }

        cvar.Reset();
        console.Write("^0F0{0}^FFF reset to {1}\n", cvar.Name, cvar.ToString());
    }

    private static void CmdCVarResetAll(string[] args)
    {
        var console = Game.console;
        
        int count = 0;
        foreach (var cvar in s_CVars.Values)
        {
            cvar.Reset();
            count++;
        }
        
        console.Write("^0F0Reset {0} cvars to default values\n", count);
    }

    private static void CmdSet(string[] args)
    {
        var console = Game.console;
        
        if (args.Length != 2)
        {
            console.Write("Usage: set <name> <value>\n");
            return;
        }

        string name = args[0];
        string value = args[1];
        
        var cvar = GetCVar(name);
        if (cvar == null)
        {
            console.Write("^F00Unknown cvar: {0}\n", name);
            return;
        }

        if (cvar.TrySetFromString(value))
        {
            console.Write("^0F0{0}^FFF = {1}\n", cvar.Name, cvar.ToString());
        }
        else
        {
            console.Write("^F00Invalid value '{0}' for {1}\n", value, name);
        }
    }

    private static void CmdCVarSave(string[] args)
    {
        var console = Game.console;
        
        string filePath = null;
        if (args.Length > 0)
            filePath = args[0];
            
        SaveConfig(filePath);
        console.Write("^0F0Config saved\n");
    }

    private static void CmdCVarLoad(string[] args)
    {
        var console = Game.console;
        
        string filePath = null;
        if (args.Length > 0)
            filePath = args[0];
            
        LoadConfig(filePath);
        console.Write("^0F0Config loaded\n");
    }

    private static void CmdCVarExec(string[] args)
    {
        var console = Game.console;
        
        if (args.Length != 1)
        {
            console.Write("Usage: cvarexec <filename>\n");
            return;
        }

        LoadConfig(args[0]);
    }
}