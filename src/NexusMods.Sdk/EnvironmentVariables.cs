using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Sdk;

/// <summary>
/// Helper methods for interacting with environment variables.
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// Tries to get a boolean value.
    /// </summary>
    /// <remarks>
    /// Supports parsing values <c>true</c>, <c>false</c>, <c>1</c>, and <c>0</c> as boolean values.
    /// </remarks>
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

    /// <summary>
    /// Tries to get an enum value.
    /// </summary>
    public static bool TryGetEnum<T>(string name, out T value) where T : struct
    {
        if (!TryGetString(name, out var sValue))
        {
            value = default(T);
            return false;
        }

        return Enum.TryParse(sValue, ignoreCase: true, out value);
    }

    /// <summary>
    /// Tries to get 
    /// </summary>
    public static bool TryGetUri(string name, [NotNullWhen(true)] out Uri? value)
    {
        if (!TryGetString(name, out var sValue))
        {
            value = null;
            return false;
        }

        return Uri.TryCreate(sValue, UriKind.Absolute, out value);
    }

    /// <summary>
    /// Tries to get a string.
    /// </summary>
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
