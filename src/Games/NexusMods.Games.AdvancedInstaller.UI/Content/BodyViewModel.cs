using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using IModContentTreeEntryVM = NexusMods.Games.AdvancedInstaller.UI.Content.Left.ITreeEntryViewModel;
using ISelectableTreeEntryVM =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry.
    ITreeEntryViewModel;
using IPreviewTreeEntryVM =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel;

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

            StartSelectObserver.DisposeWith(disposables);
            CancelSelectObserver.DisposeWith(disposables);
            DirectorySelectedObserver.DisposeWith(disposables);

            PreviewViewModel.LocationsCache.Connect()
                .WhenPropertyChanged(vm => vm.Root.MarkForRemoval)
                .Where(x => x.Value)
                .Subscribe(x =>
                {
                    PreviewViewModel.LocationsCache.RemoveKey(x.Sender.Root.FullPath.LocationId);
                    if (PreviewViewModel.Locations.Count == 0)
                        CurrentPreviewViewModel = EmptyPreviewViewModel;
                })
                .DisposeWith(disposables);

            PreviewViewModel.Locations.WhenAnyValue(x => x.Count)
                .Subscribe(x => { CanInstall = x > 0; })
                .DisposeWith(disposables);
        });
    }

    public Subject<IModContentTreeEntryVM> StartSelectObserver { get; }
    public Subject<IModContentTreeEntryVM> CancelSelectObserver { get; }
    public Subject<ISelectableTreeEntryVM> DirectorySelectedObserver { get; }

    public DeploymentData Data { get; set; } = new();
    public string ModName { get; set; }
    public IModContentViewModel ModContentViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new EmptyPreviewViewModel();
    public ISelectLocationViewModel SelectLocationViewModel { get; }

    [Reactive] public bool CanInstall { get; set; } = false;
    [Reactive] public IViewModelInterface CurrentPreviewViewModel { get; private set; }

    internal readonly List<IModContentTreeEntryVM> SelectedItems = new();

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
        return PreviewViewModel.Locations.Any(location => location.Root.Children.Count > 0);
    }

    public void OnDirectorySelected(ISelectableTreeEntryVM directory)
    {
        foreach (var item in SelectedItems)
        {
            var node = PreviewViewModel.GetOrCreateBindingTarget(item.FullPath, item.IsDirectory, directory.Path);
            item.Link(Data, node,
                false); //TODO:Implement IsFolderMerged // directory.Status == SelectableDirectoryNodeStatus.Regular);
        }

        SelectedItems.Clear();
        CurrentPreviewViewModel = PreviewViewModel;
    }
}
