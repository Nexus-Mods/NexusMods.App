using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.Icons;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IPageViewModelInterface
{
    ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections { get; }

    IconValue StateIcon { get; }

    AlertSettings AlertSettings { get; }
}
