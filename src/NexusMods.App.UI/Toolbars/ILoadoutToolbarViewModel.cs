using NexusMods.DataModel.Loadouts;

namespace NexusMods.App.UI.Toolbars;

public interface ILoadoutToolbarViewModel : IToolbarViewModel
{
    /// <summary>
    /// The caption text to display on the left side of the toolbar.
    /// </summary>
    public string Caption { get; set; }

    /// <summary>
    /// The currently loaded Loadout, may change the caption based on the name
    /// of the loadout.
    /// </summary>
    public LoadoutId LoadoutId { get; set; }

}
