using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemModel : ITreeDataGridItemModel<ILibraryItemModel, EntityId>;

[Obsolete("Use CompositeItemModel instead")]
public interface IHasTicker
{
    Observable<DateTimeOffset>? Ticker { get; set; }
}

[Obsolete("Use CompositeItemModel instead")]
public interface IHasLinkedLoadoutItems
{
    IObservable<IChangeSet<LibraryLinkedLoadoutItem.ReadOnly, EntityId>> LinkedLoadoutItemsObservable { get; }
    ObservableDictionary<EntityId, LibraryLinkedLoadoutItem.ReadOnly> LinkedLoadoutItems { get; }

    [MustDisposeResource] static IDisposable SetupLinkedLoadoutItems<TModel>(TModel self, SerialDisposable serialDisposable)
        where TModel : IHasLinkedLoadoutItems, ILibraryItemWithInstallAction, ILibraryItemWithInstalledDate
    {
        var disposable = self.LinkedLoadoutItems
            .ObserveCountChanged(notifyCurrentCount: true)
            .Subscribe(self, static (count, self) =>
            {
                var isInstalled = count > 0;
                self.IsInstalled.Value = isInstalled;
                self.InstallButtonText.Value = ILibraryItemWithInstallAction.GetButtonText(isInstalled);
                self.InstalledDate.Value = isInstalled ? self.LinkedLoadoutItems.Select(static kv => kv.Value.GetCreatedAt()).Max() : DateTimeOffset.MinValue;
            });

        if (serialDisposable.Disposable is null)
        {
            serialDisposable.Disposable = self.LinkedLoadoutItemsObservable.OnUI().SubscribeWithErrorLogging(changes =>
                {
                    self.LinkedLoadoutItems.ApplyChanges(changes);
                }
            );
        }

        return disposable;
    }
}

[Obsolete("Use CompositeItemModel instead")]
public interface IIsParentLibraryItemModel : ILibraryItemModel
{
    IReadOnlyList<LibraryItemId> LibraryItemIds { get; }
}

[Obsolete("Use CompositeItemModel instead")]
public interface IIsChildLibraryItemModel : ILibraryItemModel
{
    LibraryItemId LibraryItemId { get; }
}

[Obsolete("Use CompositeItemModel instead")]
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface ILibraryItemWithDates : IHasTicker, ILibraryItemWithDownloadedDate, ILibraryItemWithInstalledDate
{
    [MustDisposeResource]
    static IDisposable SetupDates<TModel>(TModel self) where TModel : class, ILibraryItemWithDates
    {
        return self.WhenActivated(static (self, disposables) =>
        {
            Debug.Assert(self.Ticker is not null, "should've been set before activation");
            self.Ticker.Subscribe(self, static (now, self) =>
            {
                ILibraryItemWithDownloadedDate.FormatDate(self, now: now);
                ILibraryItemWithInstalledDate.FormatDate(self, now: now);
            }).AddTo(disposables);

            ILibraryItemWithDownloadedDate.FormatDate(self, now: TimeProvider.System.GetLocalNow());
            ILibraryItemWithInstalledDate.FormatDate(self, now: TimeProvider.System.GetLocalNow());
        });
    }
}
