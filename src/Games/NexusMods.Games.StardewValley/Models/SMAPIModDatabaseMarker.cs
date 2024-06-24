using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

[Include<StoredFile>]
public partial class SMAPIModDatabaseMarker : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIModDatabaseMarker";
    
    
    public static readonly MarkerAttribute SMAPIModDatabase = new(Namespace, nameof(SMAPIModDatabase));
}
