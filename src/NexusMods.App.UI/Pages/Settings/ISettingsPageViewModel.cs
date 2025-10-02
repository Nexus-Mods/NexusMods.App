using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.Settings.Section;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.Settings;

public interface ISettingsPageViewModel : IPageViewModelInterface
{
    ReactiveCommand<Unit> SaveCommand { get; }
    ReactiveCommand<Unit> CancelCommand { get; }

    ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }

    ReadOnlyObservableCollection<ISettingSectionViewModel> Sections { get; }
}
