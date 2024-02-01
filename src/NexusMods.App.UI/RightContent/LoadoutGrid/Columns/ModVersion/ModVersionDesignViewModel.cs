using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModVersion;

public class ModVersionDesignViewModel : AViewModel<IModVersionViewModel>, IModVersionViewModel
{
    public ModCursor Row { get; set; } = Initializers.ModCursor;
    public string Version { get; } = "1.0.0-rc.1";
}
