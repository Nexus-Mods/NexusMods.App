using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.Resources;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class BodyViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel, IAdvancedInstallerCoordinator
{
    public BodyViewModel(string modName, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register, string gameName = "")
    {
        ModName = String.IsNullOrWhiteSpace(modName) ? Language.AdvancedInstaller_Manual_Mod : modName;

        StartSelectObserver = new Subject<IModContentTreeEntryViewModel>();
        CancelSelectObserver = new Subject<IModContentTreeEntryViewModel>();
        DirectorySelectedObserver = new Subject<ISelectableTreeEntryViewModel>();

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

    public Subject<IModContentTreeEntryViewModel> StartSelectObserver { get; }
    public Subject<IModContentTreeEntryViewModel> CancelSelectObserver { get; }
    public Subject<ISelectableTreeEntryViewModel> DirectorySelectedObserver { get; }

    public DeploymentData Data { get; set; } = new();
    public string ModName { get; set; }
    public IModContentViewModel ModContentViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new EmptyPreviewViewModel();
    public ISelectLocationViewModel SelectLocationViewModel { get; }

    [Reactive] public bool CanInstall { get; private set; }
    [Reactive] public IViewModelInterface CurrentPreviewViewModel { get; private set; }

    internal readonly List<IModContentTreeEntryViewModel> SelectedItems = new();

    public void OnSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        SelectedItems.Add(modContentTreeEntryViewModel);
        CurrentPreviewViewModel = SelectLocationViewModel;
    }

    public void OnCancelSelect(IModContentTreeEntryViewModel modContentTreeEntryViewModel)
    {
        SelectedItems.Remove(modContentTreeEntryViewModel);
        if (SelectedItems.Count == 0)
            CurrentPreviewViewModel = HasAnyItemsToPreview() ? PreviewViewModel : EmptyPreviewViewModel;
    }

    private bool HasAnyItemsToPreview()
    {
        return PreviewViewModel.Locations.Any(location => location.Root.Children.Count > 0);
    }

    public void OnDirectorySelected(ISelectableTreeEntryViewModel directory)
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
