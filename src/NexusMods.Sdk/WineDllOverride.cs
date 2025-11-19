using System.Collections.Immutable;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

[PublicAPI]
public readonly record struct WineDllOverride(string DllName, ImmutableArray<WineDllOverrideType> OverrideTypes)
{
    /// <summary>
    /// Gets whether the DLL is disabled.
    /// </summary>
    public bool IsDisabled => OverrideTypes is [WineDllOverrideType.Disabled];

    internal static readonly ImmutableArray<WineDllOverrideType> Disabled = [WineDllOverrideType.Disabled];

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
