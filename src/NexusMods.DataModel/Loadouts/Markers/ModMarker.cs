namespace NexusMods.DataModel.Loadouts.Markers;

public class ModMarker
{
    public LoadoutId LoadoutId { get; }
    public ModId ModId { get; }

    public ModMarker(LoadoutId loadoutId, ModId modId)
    {
        LoadoutId = loadoutId;
        ModId = modId;

    }
}
