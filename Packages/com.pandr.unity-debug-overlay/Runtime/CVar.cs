using System;
using System.Collections.Generic;
using System.Globalization;

public abstract class CVarBase {
    public readonly string name;
    public readonly string description;
    protected CVarBase(string name, string description = "") {
        this.name = name;
        this.description = description;
        CVarRegistry.Register(this);
    }
    public abstract string GetValueString();
    public abstract void SetValueString(string value);
}

public class CVarFloat : CVarBase {
    public float value;
    public readonly float defaultValue;
    public CVarFloat(string name, float defaultValue, string description = "") : base(name, description) {
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }
    public override string GetValueString() => value.ToString(CultureInfo.InvariantCulture);
    public override void SetValueString(string v) => value = float.Parse(v, CultureInfo.InvariantCulture);
}

public class CVarInt : CVarBase {
    public int value;
    public readonly int defaultValue;
    public CVarInt(string name, int defaultValue, string description = "") : base(name, description) {
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }
    public override string GetValueString() => value.ToString();
    public override void SetValueString(string v) => value = int.Parse(v, CultureInfo.InvariantCulture);
}

public class CVarString : CVarBase {
    public string value;
    public readonly string defaultValue;
    public CVarString(string name, string defaultValue, string description = "") : base(name, description) {
        this.defaultValue = defaultValue;
        this.value = defaultValue;
    }
    public override string GetValueString() => value;
    public override void SetValueString(string v) => value = v;
}

public static class CVarRegistry {
    static Dictionary<string, CVarBase> vars = new Dictionary<string, CVarBase>(StringComparer.OrdinalIgnoreCase);
    public static void Register(CVarBase cvar) => vars[cvar.name] = cvar;
    public static CVarBase Find(string name) => vars.TryGetValue(name, out var v) ? v : null;
    public static IEnumerable<CVarBase> All => vars.Values;
} 