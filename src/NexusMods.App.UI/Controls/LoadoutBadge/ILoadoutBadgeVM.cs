namespace NexusMods.App.UI.Controls.LoadoutBadge;

public interface ILoadoutBadgeVM : IViewModelInterface
{
    string LoadoutShortName { get; }
    
    bool IsLoadouotSelected { get; }
    
    bool IsLoadoutApplied { get; }
    
    bool IsLoadoutInProgress { get; }
}
