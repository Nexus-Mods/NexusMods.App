using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ILoadoutViewModel : IPageViewModelInterface
{
    LoadoutTreeDataGridAdapter Adapter { get; }

    ReactiveCommand<NavigationInformation> ViewLibraryCommand { get; }
    
    string EmptyStateTitleText { get; }

    ReactiveCommand<NavigationInformation> ViewFilesCommand { get; }

    ReactiveCommand<Unit> RemoveItemCommand { get; }
    
    ReactiveCommand<Unit> CollectionToggleCommand { get; }
    ReactiveCommand<Unit> DeselectItemsCommand { get; }
    
    public int SelectionCount { get; } 
    
    bool IsCollection { get; }
    
    bool IsCollectionEnabled { get; }
    bool IsCollectionUploaded { get; }
    
    string CollectionName { get; } 
    
    ISortingSelectionViewModel RulesSectionViewModel { get; }
    
    public int ItemCount { get; }
    
    public bool HasRulesSection { get; }
    
    public LoadoutPageSubTabs SelectedSubTab { get; }

    ReactiveCommand<Unit> CommandUploadRevision { get; }
    ReactiveCommand<Unit> CommandOpenRevisionUrl { get; }

    ReactiveCommand<Unit> CommandRenameGroup { get; }
}
