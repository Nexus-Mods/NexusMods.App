using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Networking.NexusWebApi.V1Interop;

/// <summary>
/// Maps between game domains and Game IDs.
/// </summary>
public partial class GameDomainToGameIdMapping : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusWebApi.GameDomainToGameIdMapping";
    
    /// <summary>
    /// The game's domain.
    /// </summary>
    public static readonly GameDomainAttribute Domain = new(Namespace, nameof(Domain)) { IsIndexed = true };
    
    /// <summary>
    /// The game's ID on Nexus Mods.
    /// </summary>
    public static readonly GameIdAttribute GameId = new(Namespace, nameof(GameId)) { IsIndexed = true };
}
