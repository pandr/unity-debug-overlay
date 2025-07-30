using System;
using System.Collections.Generic;
using UnityEngine;

public interface IConfigVar
{
    string Name { get; }
    string Description { get; }
    string GetStringValue();
    bool TrySetValue(string input);
    void ResetToDefault();
}

public struct ConfigVarFloat : IConfigVar
{
    private float m_Value;
    private float m_DefaultValue;
    private string m_Name;
    private string m_Description;
    
    public string Name => m_Name;
    public string Description => m_Description;
    public float Value => m_Value;
    
    public ConfigVarFloat(string name, float defaultValue, string description = "")
    {
        m_Name = name;
        m_DefaultValue = defaultValue;
        m_Description = description;
        m_Value = defaultValue;
    }
    
    public string GetStringValue()
    {
        return m_Value.ToString("F3");
    }
    
    public bool TrySetValue(string input)
    {
        if (float.TryParse(input, out float newValue))
        {
            m_Value = newValue;
            return true;
        }
        return false;
    }
    
    public void ResetToDefault()
    {
        m_Value = m_DefaultValue;
    }
}

public struct ConfigVarInt : IConfigVar
{
    private int m_Value;
    private int m_DefaultValue;
    private string m_Name;
    private string m_Description;
    
    public string Name => m_Name;
    public string Description => m_Description;
    public int Value => m_Value;
    
    public ConfigVarInt(string name, int defaultValue, string description = "")
    {
        m_Name = name;
        m_DefaultValue = defaultValue;
        m_Description = description;
        m_Value = defaultValue;
    }
    
    public string GetStringValue()
    {
        return m_Value.ToString();
    }
    
    public bool TrySetValue(string input)
    {
        if (int.TryParse(input, out int newValue))
        {
            m_Value = newValue;
            return true;
        }
        return false;
    }
    
    public void ResetToDefault()
    {
        m_Value = m_DefaultValue;
    }
}

public struct ConfigVarBool : IConfigVar
{
    private bool m_Value;
    private bool m_DefaultValue;
    private string m_Name;
    private string m_Description;
    
    public string Name => m_Name;
    public string Description => m_Description;
    public bool Value => m_Value;
    
    public ConfigVarBool(string name, bool defaultValue, string description = "")
    {
        m_Name = name;
        m_DefaultValue = defaultValue;
        m_Description = description;
        m_Value = defaultValue;
    }
    
    public string GetStringValue()
    {
        return m_Value ? "1" : "0";
    }
    
    public bool TrySetValue(string input)
    {
        if (bool.TryParse(input, out bool newValue))
        {
            m_Value = newValue;
            return true;
        }
        // Also accept "0", "1" for bool
        if (input == "0")
        {
            m_Value = false;
            return true;
        }
        if (input == "1")
        {
            m_Value = true;
            return true;
        }
        return false;
    }
    
    public void ResetToDefault()
    {
        m_Value = m_DefaultValue;
    }
}

public static class ConfigVarRegistry
{
    private static Dictionary<string, IConfigVar> s_Vars = new Dictionary<string, IConfigVar>();
    
    public static void Register<T>(ref T configVar) where T : struct, IConfigVar
    {
        s_Vars[configVar.Name] = configVar;
    }
    
    public static bool TryGetValue(string name, out IConfigVar configVar)
    {
        return s_Vars.TryGetValue(name, out configVar);
    }
    
    public static bool TrySetValue(string name, string value)
    {
        if (s_Vars.TryGetValue(name, out IConfigVar configVar))
        {
            return configVar.TrySetValue(value);
        }
        return false;
    }
    
    public static bool TryGetFloat(string name, out float value)
    {
        if (s_Vars.TryGetValue(name, out IConfigVar configVar) && configVar is ConfigVarFloat floatVar)
        {
            value = floatVar.Value;
            return true;
        }
        value = 0.0f;
        return false;
    }
    
    public static bool TryGetInt(string name, out int value)
    {
        if (s_Vars.TryGetValue(name, out IConfigVar configVar) && configVar is ConfigVarInt intVar)
        {
            value = intVar.Value;
            return true;
        }
        value = 0;
        return false;
    }
    
    public static bool TryGetBool(string name, out bool value)
    {
        if (s_Vars.TryGetValue(name, out IConfigVar configVar) && configVar is ConfigVarBool boolVar)
        {
            value = boolVar.Value;
            return true;
        }
        value = false;
        return false;
    }
    
    public static void ResetToDefault(string name)
    {
        if (s_Vars.TryGetValue(name, out IConfigVar configVar))
        {
            configVar.ResetToDefault();
        }
    }
    
    public static Dictionary<string, IConfigVar>.Enumerator GetAllVars()
    {
        return s_Vars.GetEnumerator();
    }
    
    public static int Count => s_Vars.Count;
} 