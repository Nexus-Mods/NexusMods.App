using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

[PublicAPI]
[Include<CollectionDownload>]
public partial class CollectionDownloadBundled : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownloadBundled";

    /// <summary>
    /// Bundled path.
    /// </summary>
    public static readonly RelativePathAttribute BundledPath = new(Namespace, nameof(BundledPath));
}
