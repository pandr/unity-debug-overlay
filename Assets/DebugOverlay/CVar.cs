using System;
using UnityEngine;

/// <summary>
/// A config variable that can be changed from the console.
/// Reading the value is extremely fast (direct field access performance).
/// Usage: public static readonly CVar&lt;float&gt; fov = new CVar&lt;float&gt;("fov", 90f, "Field of view");
/// Access: float currentFov = CVars.fov; // Implicit conversion, zero overhead
/// </summary>
[System.Serializable]
public partial class CVar<T> where T : IComparable<T>, IConvertible
{
    [SerializeField] private T _value;
    [SerializeField] private readonly T _defaultValue;
    [SerializeField] private readonly string _name;
    [SerializeField] private readonly string _description;
    
    private readonly Func<T, bool> _validator;
    private readonly Action<T> _onChanged;

    /// <summary>
    /// Current value of the cvar. Ultra-fast access.
    /// </summary>
    public T Value 
    { 
        get => _value;
        set => SetValue(value);
    }

    /// <summary>
    /// Default value of the cvar
    /// </summary>
    public T DefaultValue => _defaultValue;
    
    /// <summary>
    /// Name of the cvar as it appears in console
    /// </summary>
    public string Name => _name;
    
    /// <summary>
    /// Description shown in help
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// Implicit conversion for ultra-fast value access
    /// </summary>
    public static implicit operator T(CVar<T> cvar) => cvar._value;

    /// <summary>
    /// Create a new config variable. Automatically registers with CVarSystem.
    /// </summary>
    /// <param name="name">Console name (e.g., "fov")</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="description">Help description</param>
    /// <param name="validator">Optional validation function</param>
    /// <param name="onChanged">Optional callback when value changes</param>
    public CVar(string name, T defaultValue, string description = "", 
                Func<T, bool> validator = null, Action<T> onChanged = null)
    {
        _name = name?.ToLower() ?? throw new ArgumentNullException(nameof(name));
        _defaultValue = defaultValue;
        _description = description ?? "";
        _value = defaultValue;
        _validator = validator;
        _onChanged = onChanged;
        
        // Register with the system
        CVarSystem.Register(this);
    }

    /// <summary>
    /// Set the value with validation and change notification
    /// </summary>
    public bool SetValue(T newValue)
    {
        // Validate if validator exists
        if (_validator != null && !_validator(newValue))
        {
            return false;
        }

        var oldValue = _value;
        _value = newValue;
        
        // Notify if value actually changed
        if (!oldValue.Equals(newValue))
        {
            _onChanged?.Invoke(newValue);
        }
        
        return true;
    }

    /// <summary>
    /// Reset to default value
    /// </summary>
    public void Reset()
    {
        SetValue(_defaultValue);
    }

    /// <summary>
    /// Try to set value from string (used by console)
    /// </summary>
    public bool TrySetFromString(string valueStr)
    {
        try
        {
            T newValue = (T)Convert.ChangeType(valueStr, typeof(T));
            return SetValue(newValue);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get current value as string
    /// </summary>
    public override string ToString()
    {
        return _value?.ToString() ?? "null";
    }
}