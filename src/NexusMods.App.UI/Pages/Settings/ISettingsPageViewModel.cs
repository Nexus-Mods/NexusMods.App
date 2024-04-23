using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

public interface ISettingsPageViewModel : IPageViewModelInterface
{
    ReactiveCommand<Unit, Unit> SaveCommand { get; }
    ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    /// <summary>
    /// X button in the top right corner of the window.
    /// </summary>
    ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }
}
