using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;

namespace NexusMods.App.UI.Controls.Spine;

public interface ISpineViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets the left menu view model.
    /// </summary>
    public ILeftMenuViewModel? LeftMenuViewModel { get; }

    /// <summary>
    /// Gets the home button.
    /// </summary>
    public IIconButtonViewModel Home { get; }

    /// <summary>
    /// Gets the downloads button.
    /// </summary>
    public ISpineDownloadButtonViewModel Downloads { get; }

    /// <summary>
    /// Gets all loadout buttons.
    /// </summary>
    public ReadOnlyObservableCollection<IImageButtonViewModel> LoadoutSpineItems { get; }

    public void NavigateToHome();
}
