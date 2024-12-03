using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using System.Reactive.Disposables;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionsViewModel : APageViewModel<ICollectionsViewModel>, ICollectionsViewModel
{
    public CollectionsViewModel(
        IServiceProvider serviceProvider,
        IConnection conn,
        IWindowManager windowManager,
        LoadoutId targetLoadout) : base(windowManager)
    {
        TabIcon = IconValues.ModLibrary;
        TabTitle = "Collections (WIP)";

        this.WhenActivated(d =>
        {
            var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);
            var userAvatarPipeline = ImagePipelines.GetUserAvatarPipeline(serviceProvider);

            CollectionMetadata.ObserveAll(conn)
                .Transform(ICollectionCardViewModel (coll) => new CollectionCardViewModel(
                    tileImagePipeline: tileImagePipeline,
                    userAvatarPipeline: userAvatarPipeline,
                    windowManager: WindowManager,
                    workspaceId: WorkspaceId,
                    connection: conn,
                    revision: coll.Revisions.First().RevisionId,
                    targetLoadout: targetLoadout)
                )
                .Bind(out _collections)
                .Subscribe()
                .DisposeWith(d);
        });
    }

    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new(new ObservableCollection<ICollectionCardViewModel>());
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;
}
