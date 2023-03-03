using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;

namespace NexusMods.App.UI.Controls.Spine;

public interface ISpineViewModel : IViewModelInterface
{
    /// <summary>
    /// Home Button
    /// </summary>
    public IIconButtonViewModel Home { get; }

    /// <summary>
    /// Add Button
    /// </summary>
    public IIconButtonViewModel Add { get; }

    /// <summary>
    /// Game Buttons
    /// </summary>
    public ReadOnlyObservableCollection<IImageButtonViewModel> Games { get; }

}
