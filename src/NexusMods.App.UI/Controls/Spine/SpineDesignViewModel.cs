using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Controls.Spine;

public class SpineDesignViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    public ILeftMenuViewModel? LeftMenuViewModel => null;

    public IIconButtonViewModel Home { get; } = new IconButtonDesignViewModel();
    public IIconButtonViewModel AddLoadout { get; } = new IconButtonDesignViewModel();

    public ISpineDownloadButtonViewModel Downloads { get; } = new SpineDownloadButtonDesignerViewModel();

    public ReadOnlyObservableCollection<IImageButtonViewModel> LoadoutSpineItems { get; } =
        new(new ObservableCollection<IImageButtonViewModel>
    {
        new ImageButtonDesignViewModel(),
        new ImageButtonDesignViewModel(),
        new ImageButtonDesignViewModel(),
        new ImageButtonDesignViewModel(),
    });

    public void NavigateToHome() { }
}
