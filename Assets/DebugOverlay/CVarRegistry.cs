using System;
using System.Collections.Generic;

public static class CVarRegistry
{
    private static readonly Dictionary<string, object> cvars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public static void Register<T>(CVar<T> cvar)
    {
        cvars[cvar.Name] = cvar;
    }

    public static object Find(string name)
    {
        cvars.TryGetValue(name, out var cvar);
        return cvar;
    }

    public static IEnumerable<object> AllCVars() => cvars.Values;
}