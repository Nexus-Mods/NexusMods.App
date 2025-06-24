using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents a downloaded file in the file.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class DownloadedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.DownloadedFile";

    /// <summary>
    /// URI to the download page, not the direct download link.
    /// </summary>
    /// <remarks>
    /// Many direct download links aren't permalinks. Those shouldn't be stored.
    /// Instead, this is the link to the page from which the download was initiated.
    /// </remarks>
    /// <example>
    /// https://www.nexusmods.com/skyrim/mods/3863
    /// </example>
    public static readonly UriAttribute DownloadPageUri = new(Namespace, nameof(DownloadPageUri));
}
