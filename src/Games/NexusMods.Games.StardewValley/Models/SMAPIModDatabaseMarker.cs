using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

[Include<File>]
public partial class SMAPIModDatabaseMarker : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIModDatabaseMarker";
    
    
    public static readonly BooleanAttribute SMAPIModDatabase = new(Namespace, "SMAPIModDatabase");
}
