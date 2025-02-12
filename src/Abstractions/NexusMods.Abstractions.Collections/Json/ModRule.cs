using System.Diagnostics;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.Hashes;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Collections.Json;

/// <summary>
/// Vortex-style rules between two items.
/// https://github.com/Nexus-Mods/extension-collections/blob/37f6e3909809eaae954628b398d16ef1245ce940/src/types/ICollection.ts#L64-L68
/// </summary>
[DebuggerDisplay("{Type}")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ModRule
{
    /// <summary>
    /// Rule type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required VortexModRuleType Type { get; init; }

    /// <summary>
    /// Source item.
    /// </summary>
    [JsonPropertyName("source")]
    public required VortexModReference Source { get; init; }

    /// <summary>
    /// Other item.
    /// </summary>
    [JsonPropertyName("reference")]
    public required VortexModReference Other { get; init; }
}

/// <summary>
///  Vortex-style mod reference.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class VortexModReference
{

// https://github.com/Nexus-Mods/modmeta-db/blob/62fae057a2fe04f5dbc8f69c988ec6b046016a60/lib/types.d.ts#L3
#region Vortex IReference

    [JsonPropertyName("fileMD5")]
    public Md5 FileMD5 { get; init; }

    [JsonPropertyName("fileSize")]
    public Size FileSize { get; init; }

    /// <summary>
    /// This can be an exact version (1.1.12) or a version range (^1.0.0).
    /// </summary>
    /// <remarks>
    /// See this shit: https://github.com/Nexus-Mods/Vortex/blob/1bc2a0bca27353df617f5a0b0f331cf9d23eea9c/src/extensions/mod_management/util/testModReference.ts#L102-L125
    /// </remarks>
    [JsonPropertyName("versionMatch")]
    public string? VersionMatch { get; init; }

    [JsonPropertyName("logicalFileName")]
    public string? LogicalFileName { get; init; }

    [JsonPropertyName("fileExpression")]
    public string? FileExpression { get; init; }

#endregion

// https://github.com/Nexus-Mods/Vortex/blob/1bc2a0bca27353df617f5a0b0f331cf9d23eea9c/src/extensions/mod_management/types/IMod.ts#L46
#region Vortex IModReference

    /// <summary>
    /// Vortex internal ID bullshit.
    /// </summary>
    [JsonPropertyName("idHint")]
    public string? IdHint { get; init; }

    /// <summary>
    /// I have no fucking idea why this and <see cref="FileMD5"/> exists on the same object.
    /// Vortex also does some really questionable stuff: https://github.com/Nexus-Mods/Vortex/blob/1bc2a0bca27353df617f5a0b0f331cf9d23eea9c/src/extensions/mod_management/util/dependencies.ts#L37-L47
    /// This is likely never included in any JSON files.
    /// </summary>
    public Md5 MD5Hint { get; init; }

    /// <summary>
    /// Corresponds to <see cref="ModSource.Tag"/>.
    /// </summary>
    public string? Tag { get; init; }

#endregion

}

// NOTE(erri120): Vortex definition was likely just copy-pasted from modmeta-db definiton
// https://github.com/Nexus-Mods/extension-collections/blob/37f6e3909809eaae954628b398d16ef1245ce940/src/types/ICollection.ts#L62
// https://github.com/Nexus-Mods/modmeta-db/blob/62fae057a2fe04f5dbc8f69c988ec6b046016a60/lib/types.d.ts#L11
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public enum VortexModRuleType
{
    /// Source comes before Reference.
    [JsonStringEnumMemberName("before")]
    Before,

    /// <summary>
    /// Source comes after Reference.
    /// </summary>
    [JsonStringEnumMemberName("after")]
    After,

    /// <summary>
    /// TODO: unknown
    /// </summary>
    [JsonStringEnumMemberName("requires")]
    Requires,

    /// Source conflicts with Reference.
    [JsonStringEnumMemberName("conflicts")]
    Conflicts,

    /// <summary>
    /// TODO: unknown
    /// </summary>
    [JsonStringEnumMemberName("recommends")]
    Recommends,

    /// <summary>
    /// TODO: unknown
    /// </summary>
    [JsonStringEnumMemberName("provides")]
    Provides,
}
