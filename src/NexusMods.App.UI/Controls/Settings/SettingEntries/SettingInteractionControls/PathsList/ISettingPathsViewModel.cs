using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries.PathsList;

public interface ISettingPathsViewModel : ISettingInteractionControl
{
    public ConfigurablePathsContainer ConfigurablePathsContainer { get; }
}
