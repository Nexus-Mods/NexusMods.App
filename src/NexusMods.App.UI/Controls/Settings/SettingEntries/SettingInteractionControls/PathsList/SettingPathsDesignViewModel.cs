using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public class SettingPathsDesignViewModel : AViewModel<ISettingPathsViewModel>, ISettingPathsViewModel
{
    public IValueContainer ValueContainer => ConfigurablePathsContainer;
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }

    public SettingPathsDesignViewModel()
    {
        ConfigurablePathsContainer = new ConfigurablePathsContainer(
            [new ConfigurablePath(KnownPath.LocalApplicationDataDirectory, "DefaultPath")],
            [],
            null!
        );
    }
}
