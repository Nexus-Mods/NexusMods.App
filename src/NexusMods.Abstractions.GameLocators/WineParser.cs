using System.Text;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Parser for WINE.
/// </summary>
public static class WineParser
{
    /// <summary>
    /// Environment name.
    /// </summary>
    public const string WineDllOverridesEnvironmentVariableName = "WINEDLLOVERRIDES";

    /// <summary>
    /// Parses the given `winetricks.log` file and returns all installed packages.
    /// </summary>
    public static IReadOnlySet<string> ParseWinetricksLogFile(AbsolutePath filePath)
    {
        if (!filePath.FileExists) return new HashSet<string>();

        using var stream = filePath.Read();
        return ParseWinetricksLogFile(stream);
    }

    /// <summary>
    /// Parses the given `winetricks.log` file and returns all installed packages.
    /// </summary>
    public static IReadOnlySet<string> ParseWinetricksLogFile(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var result = new HashSet<string>();

        while (sr.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            result.Add(line);
        }

        return result;
    }

    /// <summary>
    /// Parses the environment variable out of a string.
    /// </summary>
    public static IReadOnlyList<WineDllOverride> ParseEnvironmentVariable(ReadOnlySpan<char> environmentVariableValue)
    {
        if (environmentVariableValue.Length == 0) return [];

        var results = new List<WineDllOverride>();

        // https://gitlab.winehq.org/wine/wine/-/wikis/Wine-User's-Guide#winedlloverrides-dll-overrides

        // NOTE(erri120): DLLs are separated with a semicolon
        var splitEnumerator = environmentVariableValue.Split(';');
        foreach (var splitRange in splitEnumerator)
        {
            var section = environmentVariableValue[splitRange];

            var index = section.LastIndexOf('=');
            if (index == -1) continue;

            var namesSpan = section[..index];

            var dllNamesEnumerator = namesSpan.Split(',');
            var dllOverrideTypes = GetOverrideTypes(section);

            foreach (var range in dllNamesEnumerator)
            {
                var name = FixDllName(namesSpan[range]).ToString();
                results.Add(new WineDllOverride(name, dllOverrideTypes));
            }
        }

        return results;
    }

    private static ReadOnlySpan<char> FixDllName(ReadOnlySpan<char> input)
    {
        var index = input.LastIndexOf(".dll", StringComparison.OrdinalIgnoreCase);
        return index == -1 ? input : input[..index];
    }

    private static WineDllOverrideType[] GetOverrideTypes(ReadOnlySpan<char> section)
    {
        var index = section.LastIndexOf('=');
        if (index == section.Length - 1) return WineDllOverride.Disabled;

        var typesSpan = section[(index + 1)..];

        var numTypes = typesSpan.Count(',') + 1;
        Span<WineDllOverrideType> overrideTypesSpan = stackalloc WineDllOverrideType[numTypes];

        var typesIndex = 0;
        var typesEnumerator = typesSpan.Split(',');
        foreach (var splitRange in typesEnumerator)
        {
            var typeSpan = typesSpan[splitRange];
            if (typeSpan.Length != 1) continue;

            var c = typeSpan[0];
            if (c is 'n') overrideTypesSpan[typesIndex++] = WineDllOverrideType.Native;
            if (c is 'b') overrideTypesSpan[typesIndex++] = WineDllOverrideType.BuiltIn;
        }

        return overrideTypesSpan[..typesIndex].ToArray();
    }

    /// <summary>
    /// Gets the DLL overrides section.
    /// </summary>
    public static ReadOnlySpan<char> GetWineDllOverridesSection(ReadOnlySpan<char> input)
    {
        const string prefix = "WINEDLLOVERRIDES=";

        var index = input.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index == -1) return ReadOnlySpan<char>.Empty;

        var span = input[(index + prefix.Length)..];

        span = span[1..];
        index = span.IndexOf('"');
        span = span[..index];

        return span;
    }
}

public record struct WineDllOverride(string DllName, WineDllOverrideType[] OverrideTypes)
{
    /// <summary>
    /// Gets whether the DLL is disabled.
    /// </summary>
    public bool IsDisabled => OverrideTypes is [WineDllOverrideType.Disabled];

    internal static readonly WineDllOverrideType[] Disabled = [WineDllOverrideType.Disabled];

    /// <inheritdoc/>
    public override string ToString()
    {
        var typesString = OverrideTypes.Select(ToString).Aggregate((a, b) => $"{a},{b}");
        return $"{DllName}={typesString}";
    }

    private static string ToString(WineDllOverrideType overrideType) => overrideType switch
    {
        WineDllOverrideType.BuiltIn => "b",
        WineDllOverrideType.Native => "n",
        _ => "",
    };
}

/// <summary>
/// DLL override types for Wine.
/// </summary>
[PublicAPI]
public enum WineDllOverrideType
{
    /// <summary>
    /// The dll is disabled.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// The built-in version should be loaded.
    /// </summary>
    BuiltIn = 1,

    /// <summary>
    /// The native version should be loaded.
    /// </summary>
    Native = 2,
}

