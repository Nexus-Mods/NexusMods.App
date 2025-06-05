using Avalonia.Platform.Storage;
using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public interface ISettingPathsViewModel : ISettingInteractionControl
{
    ConfigurablePathsContainer ConfigurablePathsContainer { get; }

    R3.ReactiveCommand CommandChangeLocation { get; }
    IStorageProvider? StorageProvider { get; set; }
}
