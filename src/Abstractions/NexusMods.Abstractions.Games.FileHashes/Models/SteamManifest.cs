using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games.FileHashes.Models;

public partial class SteamManifest : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Games.FileHashes.SteamManifest";

    
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };

}
