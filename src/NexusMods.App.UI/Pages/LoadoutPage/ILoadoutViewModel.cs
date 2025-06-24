using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ILoadoutViewModel : IPageViewModelInterface
{
    LoadoutTreeDataGridAdapter Adapter { get; }

    R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }
    
    R3.ReactiveCommand<R3.Unit> RevisionUrlLinkCommand { get; }
    
    R3.ReactiveCommand<NavigationInformation> ViewLibraryCommand { get; }
    
    string EmptyStateTitleText { get; }

    R3.ReactiveCommand<NavigationInformation> ViewFilesCommand { get; }

    R3.ReactiveCommand<R3.Unit> RemoveItemCommand { get; }
    
    R3.ReactiveCommand<R3.Unit> CollectionToggleCommand { get; }
    R3.ReactiveCommand<R3.Unit> DeselectItemsCommand { get; }
    
    public int SelectionCount { get; } 
    
    bool IsCollection { get; }
    
    bool IsCollectionEnabled { get; }
    bool IsCollectionUploaded { get; }
    
    string CollectionName { get; } 
    
    ISortingSelectionViewModel RulesSectionViewModel { get; }
    
    public int ItemCount { get; }
    
    public bool HasRulesSection { get; }
    
    public LoadoutPageSubTabs SelectedSubTab { get; }

    R3.ReactiveCommand<R3.Unit> CommandUploadRevision { get; }
}
