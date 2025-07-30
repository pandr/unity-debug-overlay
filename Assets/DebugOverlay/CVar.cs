using System;
using System.Collections.Generic;

public class CVar<T>
{
    public string Name { get; }
    public string Description { get; }
    public T DefaultValue { get; }
    private T value;
    private Action<T> onChanged;

    public T Value
    {
        get => value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(this.value, value))
            {
                this.value = value;
                onChanged?.Invoke(value);
            }
        }
    }

    public CVar(string name, T defaultValue, string description = "", Action<T> onChanged = null)
    {
        Name = name;
        DefaultValue = defaultValue;
        Description = description;
        this.onChanged = onChanged;
        this.value = defaultValue;
        CVarRegistry.Register(this);
    }

    public void Reset()
    {
        Value = DefaultValue;
    }

    public void SetFromString(string str)
    {
        object parsedVal;
        var t = typeof(T);
        if (t == typeof(int))
            parsedVal = int.Parse(str);
        else if (t == typeof(float))
            parsedVal = float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        else if (t == typeof(bool))
            parsedVal = bool.Parse(str);
        else if (t == typeof(string))
            parsedVal = str;
        else
            throw new Exception($"Unsupported CVar type: {t}");
        Value = (T)parsedVal;
    }
}