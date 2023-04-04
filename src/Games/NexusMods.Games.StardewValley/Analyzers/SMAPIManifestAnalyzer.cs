using System.Runtime.CompilerServices;
using System.Text.Json;
using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Analyzers;

/// <summary>
/// <see cref="IFileAnalyzer"/> for mods that use the Stardew Modding API (SMAPI).
/// This looks for <c>manifest.json</c> files and returns <see cref="SMAPIManifest"/>.
/// </summary>
public class SMAPIManifestAnalyzer : IFileAnalyzer
{
    public IEnumerable<FileType> FileTypes => new[] { FileType.JSON };

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken token = default)
    {
        if (!info.FileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            yield break;

        var result = await JsonSerializer.DeserializeAsync<SMAPIManifest>(info.Stream, cancellationToken: token);
        if (result is null) yield break;

        yield return result;
    }
}

// TODO: SMAPI uses ISemanticVersion instead of Version (https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Toolkit.CoreInterfaces/ISemanticVersion.cs#L7)

/// <summary>
/// https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit/Serialization/Models/Manifest.cs#L11
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Games.StardewValley.SMAPIManifest")]
public record SMAPIManifest : IFileAnalysisData
{
    /// <summary>
    /// The mod name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The unique mod ID.
    /// </summary>
    public required string UniqueID { get; init; }

    /// <summary>
    /// The mod version.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// The minimum SMAPI version required by this mod (if any).
    /// </summary>
    public required Version MinimumApiVersion { get; init; }

    /// <summary>
    /// The other mods that must be loaded before this mod.
    /// </summary>
    public SMAPIManifestDependency[]? Dependencies { get; init; }
}

/// <summary>
/// https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Toolkit.CoreInterfaces/IManifestDependency.cs#L4
/// </summary>
[PublicAPI]
[JsonName("NexusMods.Games.StardewValley.SMAPIManifestDependency")]
public record SMAPIManifestDependency
{
    /// <summary>
    /// The unique mod ID to require.
    /// </summary>
    public required string UniqueID { get; init; }

    /// <summary>
    /// The minimum required version (if any).
    /// </summary>
    public Version? MinimumVersion { get; init; }

    /// <summary>
    /// Whether the dependency must be installed to use the mod.
    /// </summary>
    public required bool IsRequired { get; init; }
}
