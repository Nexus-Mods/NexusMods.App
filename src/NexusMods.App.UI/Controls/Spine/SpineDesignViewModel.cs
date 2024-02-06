using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;

namespace NexusMods.App.UI.Controls.Spine;

public class SpineDesignViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    public ILeftMenuViewModel? LeftMenuViewModel => null;

    public IIconButtonViewModel Home { get; } = new IconButtonDesignViewModel();

    public ISpineDownloadButtonViewModel Downloads { get; } = new SpineDownloadButtonDesignerViewModel();

    public ReadOnlyObservableCollection<IImageButtonViewModel> Loadouts { get; } =
        new(new ObservableCollection<IImageButtonViewModel>
    {
        new ImageButtonDesignViewModel(),
        new ImageButtonDesignViewModel(),
    });
}
