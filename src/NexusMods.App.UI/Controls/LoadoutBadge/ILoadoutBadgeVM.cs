namespace NexusMods.App.UI.Controls.LoadoutBadge;

public interface ILoadoutBadgeVM : IViewModelInterface
{
    string LoadoutShortName { get; }
    
    bool IsLoadoutSelected { get; }
    
    bool IsLoadoutApplied { get; }
    
    bool IsLoadoutInProgress { get; }
}
