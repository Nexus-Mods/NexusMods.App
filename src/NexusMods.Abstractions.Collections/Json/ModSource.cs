using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Collections.Json;

/// <summary>
/// Polymorphic source
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ModSource
{
    /// <summary>
    /// Type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ModSourceType Type { get; init; } 

    /// <summary>
    /// Update policy.
    /// </summary>
    [JsonPropertyName("updatePolicy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UpdatePolicy UpdatePolicy { get; init; }

    /// <summary>
    /// For <see cref="ModSourceType.NexusMods"/>: Nexus Mods Mod ID.
    /// </summary>
    [JsonPropertyName("modId")]
    public ModId ModId { get; init; }

    /// <summary>
    /// For <see cref="ModSourceType.NexusMods"/>: Nexus Mods File ID.
    /// </summary>
    [JsonPropertyName("fileId")]
    public FileId FileId { get; init; }

    /// <summary>
    /// MD5 hash, present in <see cref="ModSourceType.NexusMods"/>,
    /// <see cref="ModSourceType.Browse"/>, and <see cref="ModSourceType.Direct"/>.
    /// </summary>
    [JsonPropertyName("md5")]
    public Md5HashValue Md5 { get; init; }

    /// <summary>
    /// Present in <see cref="ModSourceType.Browse"/> and <see cref="ModSourceType.Direct"/>,
    /// url to the download page.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; init; }

    /// <summary>
    /// The name of the mod in the installed loadout
    /// </summary>
    [JsonPropertyName("logicalFilename")]
    public string? LogicalFilename { get; init; }

    [JsonPropertyName("fileSize")]
    public Size FileSize { get; init; }
    
    [JsonPropertyName("fileExpression")]
    public RelativePath FileExpression { get; init; } = default;

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }
}
