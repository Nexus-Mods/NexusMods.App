using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ILoadoutViewModel : IPageViewModelInterface
{
    string EmptyStateTitleText { get; }

    LoadoutTreeDataGridAdapter Adapter { get; }
    IReadOnlyBindableReactiveProperty<int> ItemCount { get; }
    IReadOnlyBindableReactiveProperty<int> SelectionCount { get; } 

    LoadoutPageSubTabs SelectedSubTab { get; }
    bool HasRulesSection { get; }
    ISortingSelectionViewModel RulesSectionViewModel { get; }

    bool IsCollection { get; }
    IReadOnlyBindableReactiveProperty<bool> IsCollectionUploaded { get; }
    IReadOnlyBindableReactiveProperty<string> CollectionName { get; } 
    IReadOnlyBindableReactiveProperty<CollectionStatus> CollectionStatus { get; }
    IReadOnlyBindableReactiveProperty<RevisionStatus> RevisionStatus { get; }
    IReadOnlyBindableReactiveProperty<RevisionNumber> RevisionNumber { get; }

    ReactiveCommand<NavigationInformation> CommandOpenLibraryPage { get; }
    ReactiveCommand<NavigationInformation> CommandOpenFilesPage { get; }

    ReactiveCommand<Unit> CommandRemoveItem { get; }
    ReactiveCommand<Unit> CommandDeselectItems { get; }

    ReactiveCommand<Unit> CommandUploadRevision { get; }
    ReactiveCommand<Unit> CommandOpenRevisionUrl { get; }
    ReactiveCommand<Unit> CommandRenameGroup { get; }
}
