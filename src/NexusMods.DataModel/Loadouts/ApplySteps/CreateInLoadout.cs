using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;


/// <summary>
/// Specifies a new file to be added to the loadout because something else created the file in the game folders
/// </summary>
public class CreateInLoadout : IApplyStep
{
    
    public required AbsolutePath To { get; init; }
    
    public required Hash Hash { get; init; }
    
    public required Size Size { get; init; }
    
}
