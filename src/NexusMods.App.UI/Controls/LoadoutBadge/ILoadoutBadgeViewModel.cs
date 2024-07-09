namespace NexusMods.App.UI.Controls.LoadoutBadge;

public interface ILoadoutBadgeViewModel : IViewModelInterface
{
    string LoadoutShortName { get; }
    
    bool IsLoadoutSelected { get; }
    
    bool IsLoadoutApplied { get; }
    
    bool IsLoadoutInProgress { get; }
}
