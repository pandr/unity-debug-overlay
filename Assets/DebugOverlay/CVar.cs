using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quake-style configuration variables ("CVars").
/// These are intended for very cheap runtime access (one static field read)
/// while still being discoverable and editable from the in-game console.
/// </summary>

#region Registry

/// <summary>
/// Static global registry of all CVars. Every CVar registers itself in its
/// constructor. The registry is only used by the console or tooling – normal
/// runtime code never goes through it.
/// </summary>
public static class CVarRegistry
{
    static readonly Dictionary<string, CVarBase> s_Map = new Dictionary<string, CVarBase>(StringComparer.OrdinalIgnoreCase);
    static readonly List<CVarBase> s_List = new List<CVarBase>();

    internal static void Register(CVarBase v)
    {
        if (s_Map.ContainsKey(v.Name))
        {
            Debug.LogWarning($"CVar '{v.Name}' already registered – ignoring duplicate.");
            return;
        }
        s_Map.Add(v.Name, v);
        s_List.Add(v);
    }

    public static bool TryGet(string name, out CVarBase v) => s_Map.TryGetValue(name, out v);
    public static IEnumerable<CVarBase> All => s_List;

    /// <summary>
    /// Adds console commands for every registered CVar and the helper "cvar" command.
    /// Call this once after the Console instance is created.
    /// </summary>
    public static void RegisterConsoleCommands(Console console)
    {
        // Per-CVar command: same name as the variable.
        foreach (var cvar in s_List)
        {
            var local = cvar; // avoid modified-closure issue
            console.AddCommand(local.Name, (args) => local.HandleConsoleCommand(args), local.Description);
        }

        // Helper command: "cvar list" (extend with save/load in the future).
        console.AddCommand("cvar", (args) => CmdCVar(console, args), "cvar list – list all CVars");
    }

    static void CmdCVar(Console console, string[] args)
    {
        if (args.Length == 0 || string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var v in s_List)
                console.Write("  {0,-15} = {1}  {2}\n", v.Name, v.ValueString, v.Description);
            return;
        }

        console.Write("Unknown cvar argument. Try 'cvar list'\n");
    }
}
#endregion

#region Base class
/// <summary>
/// Non-generic abstract base so that the registry can store different CVar
/// types in one collection.
/// </summary>
public abstract class CVarBase
{
    public readonly string Name;
    public readonly string Description;

    protected CVarBase(string name, string description)
    {
        Name = name;
        Description = description;
        CVarRegistry.Register(this);
    }

    public abstract string ValueString { get; }
    public abstract void SetValueFromString(string value);

    internal void HandleConsoleCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Game.console.Write("{0} = {1}\n", Name, ValueString);
            return;
        }

        // Accept syntaxes: "var 123", "var = 123"
        int valueIndex = 0;
        if (args[0] == "=")
        {
            if (args.Length < 2)
            {
                Game.console.Write("Expected value after '='\n");
                return;
            }
            valueIndex = 1;
        }
        SetValueFromString(args[valueIndex]);
        Game.console.Write("{0} set to {1}\n", Name, ValueString);
    }
}
#endregion

#region Primitive specialisations
public sealed class CVarFloat : CVarBase
{
    float _value;
    public CVarFloat(string name, float defaultValue, string description = null) : base(name, description)
    {
        _value = defaultValue;
    }

    public float Value => _value;
    public static implicit operator float(CVarFloat v) => v._value;
    public override string ValueString => _value.ToString();
    public override void SetValueFromString(string s)
    {
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
            _value = f;
        else
            Game.console.Write("Could not parse float: {0}\n", s);
    }
}

public sealed class CVarInt : CVarBase
{
    int _value;
    public CVarInt(string name, int defaultValue, string description = null) : base(name, description)
    {
        _value = defaultValue;
    }
    public int Value => _value;
    public static implicit operator int(CVarInt v) => v._value;
    public override string ValueString => _value.ToString();
    public override void SetValueFromString(string s)
    {
        if (int.TryParse(s, out var i))
            _value = i;
        else
            Game.console.Write("Could not parse int: {0}\n", s);
    }
}

public sealed class CVarBool : CVarBase
{
    bool _value;
    public CVarBool(string name, bool defaultValue, string description = null) : base(name, description)
    {
        _value = defaultValue;
    }
    public bool Value => _value;
    public static implicit operator bool(CVarBool v) => v._value;
    public override string ValueString => _value ? "true" : "false";
    public override void SetValueFromString(string s)
    {
        if (bool.TryParse(s, out var b))
            _value = b;
        else if (int.TryParse(s, out var i))
            _value = i != 0;
        else
            Game.console.Write("Could not parse bool: {0}\n", s);
    }
}

public sealed class CVarString : CVarBase
{
    string _value;
    public CVarString(string name, string defaultValue, string description = null) : base(name, description)
    {
        _value = defaultValue;
    }
    public string Value => _value;
    public static implicit operator string(CVarString v) => v._value;
    public override string ValueString => _value;
    public override void SetValueFromString(string s) => _value = s;
}
#endregion
