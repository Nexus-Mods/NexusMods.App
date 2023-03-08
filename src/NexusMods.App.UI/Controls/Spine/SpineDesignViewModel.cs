using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;
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

    public IObservable<SpineButtonAction> Actions { get; } =
        new Subject<SpineButtonAction>();

    public Subject<SpineButtonAction> Activations { get; } = new();
    public ILeftMenuViewModel LeftMenu { get; set; }
}
