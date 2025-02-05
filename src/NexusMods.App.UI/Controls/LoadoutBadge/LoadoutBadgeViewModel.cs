using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public class LoadoutBadgeViewModel : AViewModel<ILoadoutBadgeViewModel>, ILoadoutBadgeViewModel
{
 
    [Reactive] public Optional<Loadout.ReadOnly> LoadoutValue { get; set; }
    
    public LoadoutBadgeViewModel(IConnection conn, ISynchronizerService syncService, bool hideOnSingleLoadout = false)
    {
        this.WhenActivated(d =>
        {
            var applyStatusSerialDisposable = new SerialDisposable().DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.LoadoutValue)
                .Where(l => l.HasValue)
                .Select(l => l!.Value)
                .Do(loadout =>
                {
                    LoadoutShortName = loadout.ShortName;
                    
                    applyStatusSerialDisposable.Disposable = Observable.FromAsync(() => syncService.StatusForLoadout(loadout.LoadoutId))
                    .Switch()
                    .OnUI()
                    .Do(applyStatus =>
                        {
                            IsLoadoutApplied = applyStatus is LoadoutSynchronizerState.Current or LoadoutSynchronizerState.NeedsSync;
                            IsLoadoutInProgress = applyStatus is LoadoutSynchronizerState.Pending;
                        }
                    )
                    .SubscribeWithErrorLogging();;
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
            
            if (hideOnSingleLoadout)
            {
                var startingLoadouts = Loadout.All(conn.Db);
                Loadout.ObserveAll(conn)
                    .Filter(l => l.InstallationId == LoadoutValue.Value.InstallationId)
                    .Count()
                    .Select(count => count > 1)
                    .OnUI()
                    .SubscribeWithErrorLogging(isVisible => IsVisible = isVisible)
                    .DisposeWith(d);
            }
        });
    }
    [Reactive] public string LoadoutShortName { get; private set; } = " ";
    [Reactive] public bool IsLoadoutApplied { get; private set; } = false;
    [Reactive] public bool IsLoadoutInProgress { get; set; } = false;
    [Reactive] public bool IsLoadoutSelected { get; set; } = true;
    [Reactive] public bool IsVisible { get; set; } = true;
}
