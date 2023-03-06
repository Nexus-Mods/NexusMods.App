using System.Collections.ObjectModel;
using System.Reactive.Subjects;
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


    /// <summary>
    /// Actions invoked by buttons on this spine
    /// </summary>
    public IObservable<SpineButtonAction> Actions { get; }

    /// <summary>
    /// Incoming activations from buttons on this spine
    /// </summary>
    public Subject<SpineButtonAction> Activations { get; }

}
