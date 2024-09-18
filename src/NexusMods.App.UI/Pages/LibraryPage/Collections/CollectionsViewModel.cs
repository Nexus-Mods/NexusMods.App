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

public class CollectionsViewModel : AViewModel<ICollectionsViewModel>, ICollectionsViewModel
{
    private readonly IConnection _conn;

    public CollectionsViewModel(IConnection conn)
    {
        _conn = conn;

        this.WhenActivated(d =>
            {
                Collection.ObserveAll(conn)
                    .Transform(coll => (ICollectionCardViewModel)new CollectionCardViewModel(conn, coll.Revisions.First().RevisionId))
                    .Bind(out _collections)
                    .Subscribe()
                    .DisposeWith(d);
            }
        );
    }

    private CollectionRevision.ReadOnly SelectLatest(IGroup<CollectionRevision.ReadOnly, EntityId, CollectionSlug> group)
    {
        return group.Cache.Items.MaxBy(r => r.RevisionNumber);
    }

    public IconValue TabIcon { get; } = IconValues.ModLibrary;
    public string TabTitle { get; } = "Collections (WIP)";
    public WindowId WindowId { get; set; }
    public WorkspaceId WorkspaceId { get; set; }
    public PanelId PanelId { get; set; }
    public PanelTabId TabId { get; set; }
    public bool CanClose()
    {
        return true;
    }

    private ReadOnlyObservableCollection<ICollectionCardViewModel> _collections = new(new ObservableCollection<ICollectionCardViewModel>());
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections => _collections;
}
