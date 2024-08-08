using Avalonia.Media;
using NexusMods.App.UI.Controls.LoadoutBadge;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public interface IImageButtonViewModel : ISpineItemViewModel
{

    /// <summary>
    /// Name for the tooltip on the button
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Image for the button
    /// </summary>
    public IImage Image { get; set; }
    
    /// <summary>
    /// ViewModel for the loadout badge
    /// </summary>
    public ILoadoutBadgeViewModel? LoadoutBadgeViewModel { get; set; }
}
