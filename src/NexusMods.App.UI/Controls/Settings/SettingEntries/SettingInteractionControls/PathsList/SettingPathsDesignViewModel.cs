using Avalonia.Platform.Storage;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using R3;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public class SettingPathsDesignViewModel : AViewModel<ISettingPathsViewModel>, ISettingPathsViewModel
{
    public IValueContainer ValueContainer => ConfigurablePathsContainer;
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }

    public IStorageProvider? StorageProvider { get; set; }
    public ReactiveCommand CommandChangeLocation { get; } = new();

    public SettingPathsDesignViewModel()
    {
        ConfigurablePathsContainer = new ConfigurablePathsContainer(
            [new ConfigurablePath(KnownPath.LocalApplicationDataDirectory, "DefaultPath")],
            [],
            null!
        );
    }
}
