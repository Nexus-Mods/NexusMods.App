using System.Reactive.Disposables;
using System.Reactive.Subjects;
using DynamicData.Binding;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Games.FOMOD.UI;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using IModContentTreeEntryVM = NexusMods.Games.AdvancedInstaller.UI.Content.Left.ITreeEntryViewModel;
using ISelectableTreeEntryVM =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry.
    ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

internal class BodyViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel, IAdvancedInstallerCoordinator
{
    public BodyViewModel(string modName, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register, string gameName = "")
    {
        ModName = modName;

        StartSelectObserver = new Subject<IModContentTreeEntryVM>();
        CancelSelectObserver = new Subject<IModContentTreeEntryVM>();
        DirectorySelectedObserver = new Subject<ISelectableTreeEntryVM>();

        ModContentViewModel = new ModContentViewModel(archiveFiles, this);
        SelectLocationViewModel = new SelectLocationViewModel(register, this, gameName);
        CurrentPreviewViewModel = EmptyPreviewViewModel;

        this.WhenActivated(disposables =>
        {
            StartSelectObserver.SubscribeWithErrorLogging(OnSelect).DisposeWith(disposables);
            CancelSelectObserver.SubscribeWithErrorLogging(OnCancelSelect).DisposeWith(disposables);
            DirectorySelectedObserver.SubscribeWithErrorLogging(OnDirectorySelected).DisposeWith(disposables);
        });
    }

    public ISubject<IModContentTreeEntryVM> StartSelectObserver { get; }
    public ISubject<IModContentTreeEntryVM> CancelSelectObserver { get; }
    public ISubject<ISelectableTreeEntryVM> DirectorySelectedObserver { get; }
    public DeploymentData Data { get; set; } = new();
    public string ModName { get; set; }
    public IModContentViewModel ModContentViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new EmptyPreviewViewModel();
    public ISelectLocationViewModel SelectLocationViewModel { get; }

    [Reactive] public bool CanInstall { get; set; } = false;
    [Reactive] public IViewModel CurrentPreviewViewModel { get; set; }

    internal readonly List<IModContentTreeEntryVM> SelectedItems = new();

    internal IObservableCollection<IModContentTreeEntryVM> LinkedItems { get; } =
        new ObservableCollectionExtended<IModContentTreeEntryVM>();

    public void OnSelect(IModContentTreeEntryVM treeEntryViewModel)
    {
        SelectedItems.Add(treeEntryViewModel);
        CurrentPreviewViewModel = SelectLocationViewModel;
    }

    public void OnCancelSelect(IModContentTreeEntryVM treeEntryViewModel)
    {
        SelectedItems.Remove(treeEntryViewModel);
        if (SelectedItems.Count == 0)
            CurrentPreviewViewModel = HasAnyItemsToPreview() ? PreviewViewModel : EmptyPreviewViewModel;
    }

    private bool HasAnyItemsToPreview()
    {
        return LinkedItems.Count > 0;
        // foreach (var location in PreviewViewModel.Locations)
        // {
        //     if (location.Root.Children.Count > 0)
        //         return true;
        // }
        //
        // return false;
    }

    public void OnDirectorySelected(ISelectableTreeEntryVM directory)
    {
        foreach (var item in SelectedItems)
        {
            var node = PreviewViewModel.GetOrCreateBindingTarget(directory.Path);
            item.Link(Data, node, directory.Status == SelectableDirectoryNodeStatus.Regular);
            LinkedItems.Add(item);
        }

        SelectedItems.Clear();
        CurrentPreviewViewModel = PreviewViewModel;
    }
}
