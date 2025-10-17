using Avalonia.Platform.Storage;
using NexusMods.UI.Sdk.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public interface ISettingPathsViewModel : IInteractionControl
{
    ConfigurablePathsContainer ConfigurablePathsContainer { get; }

    R3.ReactiveCommand CommandChangeLocation { get; }
    IStorageProvider? StorageProvider { get; set; }
}
