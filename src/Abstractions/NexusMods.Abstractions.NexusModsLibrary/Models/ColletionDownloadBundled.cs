using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

[PublicAPI]
[Include<CollectionDownload>]
public partial class ColletionDownloadBundled : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownloadBundled";

    public static readonly RelativePathAttribute BundledPath = new(Namespace, nameof(BundledPath));
}
