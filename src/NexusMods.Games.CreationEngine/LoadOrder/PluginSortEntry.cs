using NexusMods.Abstractions.Games;
using NexusMods.Games.CreationEngine.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.CreationEngine.LoadOrder;

[Include<SortOrderItem>]
public partial class PluginSortEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.CreationEngine.PluginSortEntry";
    
    public static readonly ModKeyAttribute ModKey = new(Namespace, nameof(ModKey));
    
}
