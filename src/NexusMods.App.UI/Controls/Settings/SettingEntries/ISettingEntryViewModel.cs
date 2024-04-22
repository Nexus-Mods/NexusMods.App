namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public interface ISettingEntryViewModel : IViewModelInterface
{
    string DisplayName { get; }
    string Description { get; }
    bool RequiresRestart { get; }
    
    IViewModelInterface? InteractionControlViewModel { get; }
}
