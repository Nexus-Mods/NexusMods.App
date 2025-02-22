using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// <see cref="CollectionDownload"/> for an external file.
/// </summary>
/// <remarks>
/// Source = `browse` and `direct` in the collection JSON file.
/// </remarks>
[PublicAPI]
[Include<CollectionDownload>]
public partial class CollectionDownloadExternal : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownloadExternal";

    /// <summary>
    /// MD5 hash of the file.
    /// </summary>
    public static readonly Md5Attribute Md5 = new(Namespace, nameof(Md5)) { IsIndexed = true };

    /// <summary>
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// Uri to the file.
    /// </summary>
    public static readonly UriAttribute Uri = new(Namespace, nameof(Uri));

    /// <summary>
    /// Whether the file can only be downloaded manually by the user and can't be
    /// downloaded automatically by the app.
    /// </summary>
    public static readonly MarkerAttribute ManualOnly = new(Namespace, nameof(ManualOnly)) { IsIndexed = true };
}
