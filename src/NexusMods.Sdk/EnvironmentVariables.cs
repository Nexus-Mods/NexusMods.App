using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Sdk;

internal static class EnvironmentVariables
{
    public static bool TryGetBoolean(string name, out bool value)
    {
        if (!TryGetString(name, out var sValue))
        {
            value = false;
            return false;
        }

        if (bool.TryParse(sValue, out value)) return true;

        var span = sValue.AsSpan();
        if (span.Length != 1) return false;

        var c = span[0];
        switch (c)
        {
            case '0':
                value = false;
                return true;
            case '1':
                value = true;
                return true;
            default:
                return false;
        }
    }

    public static bool TryGetEnum<T>(string name, out T value) where T : struct
    {
        if (!TryGetString(name, out var sValue))
        {
            value = default(T);
            return false;
        }

        return Enum.TryParse(sValue, ignoreCase: true, out value);
    }
    
    public static bool TryGetString(string name, [NotNullWhen(true)] out string? value)
    {
        try
        {
            var variable = Environment.GetEnvironmentVariable(name, target: EnvironmentVariableTarget.Process);
            if (variable is null)
            {
                value = null;
                return false;
            }

            value = variable;
            return true;
        }
        catch (Exception e)
        {
            Debugger.Log(level: 1, category: "Exception", message: $"Exception getting environment variable name=`{name}`: {e}");

            value = null;
            return false;
        }
    }
}
