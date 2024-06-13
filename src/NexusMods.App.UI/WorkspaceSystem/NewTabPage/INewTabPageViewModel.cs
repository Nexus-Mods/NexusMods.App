using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Banners;
using NexusMods.Icons;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageViewModel : IPageViewModelInterface
{
    ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections { get; }

    IconValue StateIcon { get; }

    BannerSettingsWrapper BannerSettingsWrapper { get; }
}
