using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.LeftMenu;

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
    /// Download Button
    /// </summary>
    public IDownloadButtonViewModel Downloads { get; }

    /// <summary>
    /// Game Buttons
    /// </summary>
    public ReadOnlyObservableCollection<IImageButtonViewModel> Games { get; }


    /// <summary>
    /// Actions invoked by buttons on this spine
    /// </summary>
    public IObservable<SpineButtonAction> Actions { get; }

    /// <summary>
    /// Incoming activations from buttons on this spine
    /// </summary>
    public Subject<SpineButtonAction> Activations { get; }

    /// <summary>
    /// View Model for the left menu, that sits between the spine and the right content
    /// </summary>
    public ILeftMenuViewModel LeftMenu { get; set; }

}
