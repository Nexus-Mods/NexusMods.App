using NexusMods.Abstractions.Games.FileHashes.Attributes.Gog;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

public partial class GogDepot : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.GogDepot";
    
    public static readonly ProductIdAttribute ProductId = new(Namespace, nameof(ProductId)) { IsIndexed = true };

    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    public static readonly SizeAttribute CompressedSize = new(Namespace, nameof(CompressedSize));
    
    /// <summary>
    /// The manifest pointed to by this depot
    /// </summary>
    public static readonly ReferenceAttribute<GogManifest> Manifest = new(Namespace, nameof(Manifest));
}
