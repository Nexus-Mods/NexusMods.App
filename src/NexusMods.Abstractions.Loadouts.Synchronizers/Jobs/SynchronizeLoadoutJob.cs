using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Loadouts;
using R3;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Synchronize the loadout with the game folder,
/// any changes in the game folder will be added to the loadout,
/// and any new changes in the loadout will be applied to the game folder.
/// </summary>
public record SynchronizeLoadoutJob(LoadoutId LoadoutId, BindableReactiveProperty<string> StatusMessage) : IJobDefinition<Unit>
{
    public SynchronizeLoadoutJob(LoadoutId loadoutId) : this(loadoutId, new BindableReactiveProperty<string>()) {}
    
    public void SetStatus(string message)
    {
        try
        {
            StatusMessage.Value = message;
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
    
