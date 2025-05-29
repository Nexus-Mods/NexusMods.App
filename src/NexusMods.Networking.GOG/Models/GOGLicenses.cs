using NexusMods.Abstractions.Games.FileHashes.Attributes.Gog;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.GOG.Models;

public partial class GOGLicense : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.GOG.GOGLicenses";
    
    /// <summary>
    /// The product ID of the GOG license.
    /// </summary>
    public static readonly ProductIdAttribute ProductId = new(Namespace, nameof(ProductId));
}
