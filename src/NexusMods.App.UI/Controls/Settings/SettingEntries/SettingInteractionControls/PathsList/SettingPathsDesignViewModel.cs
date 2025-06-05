using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public class SettingPathsDesignViewModel : AViewModel<ISettingPathsViewModel>, ISettingPathsViewModel
{
    public IValueContainer ValueContainer => ConfigurablePathsContainer;
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }
    
    [Reactive] public bool HasChanged { get; private set;  }

    public SettingPathsDesignViewModel()
    {
        ConfigurablePathsContainer = new ConfigurablePathsContainer(
            [
                new ConfigurablePath(KnownPath.ApplicationDataDirectory, "Path/To/Somewhere"),
                new ConfigurablePath(KnownPath.HomeDirectory, "Path/To/Somewhere/Else"),
            ],
            [
                new ConfigurablePath(KnownPath.ApplicationDataDirectory, "DefaultPath"),
            ],
            null!
        );


    }
}
