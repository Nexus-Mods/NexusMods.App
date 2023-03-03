using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.ViewModels;

namespace NexusMods.App.UI.Controls.Spine;

public class SpineDesignViewModel : AViewModel<ISpineViewModel>, ISpineViewModel
{
    public IIconButtonViewModel Home { get; } = new IconButtonDesignViewModel();
    public IIconButtonViewModel Add { get; } = new IconButtonViewModel();
    public ReadOnlyObservableCollection<IImageButtonViewModel> Games { get; } =
        new(new ObservableCollection<IImageButtonViewModel>
    {
        new ImageButtonDesignViewModel(),
        new ImageButtonDesignViewModel(),
    });
}
