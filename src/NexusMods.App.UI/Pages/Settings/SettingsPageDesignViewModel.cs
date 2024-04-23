using System.Collections.ObjectModel;
using System.Reactive;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

public class SettingsPageDesignViewModel : APageViewModel<ISettingsPageViewModel>, ISettingsPageViewModel
{
    public ReactiveCommand<Unit, Unit> SaveCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> CancelCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> CloseCommand { get; } = ReactiveCommand.Create(() => { });
    public ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }

    public SettingsPageDesignViewModel() : base(new DesignWindowManager())
    {
        SettingEntries = new ReadOnlyObservableCollection<ISettingEntryViewModel>([
                new SettingEntryDesignViewModel(),
                new SettingEntryDesignViewModel(),
                new SettingEntryDesignViewModel(),
                new SettingEntryDesignViewModel(),
            ]
        );
    }


}
