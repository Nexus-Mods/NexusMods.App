using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.Generic.IntrinsicFiles.Models;

public partial class IniFileDefinition : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.Generic.IniFileDefinition";
    
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
    
    public static readonly GamePathParentAttribute Path = new(Namespace, nameof(Path)) { IsIndexed = true };
}
