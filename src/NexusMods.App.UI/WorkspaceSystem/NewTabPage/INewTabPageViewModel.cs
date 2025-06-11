using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IPageViewModelInterface
{
    ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections { get; }

    AlertSettingsWrapper AlertSettingsWrapper { get; }
}
