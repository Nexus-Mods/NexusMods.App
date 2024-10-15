using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AvaloniaEdit.Utils;
using NexusMods.Abstractions.NexusWebApi.Types;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionsViewModel : APageViewModel<ICollectionsViewModel>, ICollectionsViewModel
{
    public CollectionsViewModel(
        IServiceProvider serviceProvider,
        IConnection conn,
        IWindowManager windowManager) : base(windowManager)
    {
        TabIcon = IconValues.ModLibrary;
        TabTitle = "Collections (WIP)";

        this.WhenActivated(d =>
        {
            var tileImagePipeline = ImagePipelines.GetCollectionTileImagePipeline(serviceProvider);

            CollectionMetadata.ObserveAll(conn)
                .Transform(ICollectionCardViewModel (coll) => new CollectionCardViewModel(tileImagePipeline, WindowManager, WorkspaceId, conn, coll.Revisions.First().RevisionId))
                .Bind(out _collections)
                .Subscribe()
                .DisposeWith(d);
        });
    }

    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new(new ObservableCollection<ICollectionCardViewModel>());
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;
}
