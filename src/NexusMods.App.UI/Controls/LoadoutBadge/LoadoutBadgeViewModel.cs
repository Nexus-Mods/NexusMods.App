using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public class LoadoutBadgeViewModel : AViewModel<ILoadoutBadgeViewModel>, ILoadoutBadgeViewModel
{
 
    [Reactive] public Optional<Loadout.ReadOnly> LoadoutValue { get; set; }
    
    public LoadoutBadgeViewModel(IConnection conn, ISynchronizerService syncService)
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
                    
                    applyStatusSerialDisposable.Disposable = syncService
                        .StatusFor(loadout.LoadoutId)
                        .Do(applyStatus =>
                            {
                                IsLoadoutApplied = applyStatus == LoadoutSynchronizerState.Current;
                                IsLoadoutInProgress = applyStatus == LoadoutSynchronizerState.Pending;
                            }
                        )
                        .SubscribeWithErrorLogging();
                    
                    applyStatusSerialDisposable.DisposeWith(d);

                })
                .SubscribeWithErrorLogging()
                .DisposeWith(d);
        });
    }
    [Reactive] public string LoadoutShortName { get; private set; } = " ";
    [Reactive] public bool IsLoadoutApplied { get; private set; } = false;
    [Reactive] public bool IsLoadoutInProgress { get; set; } = false;
    [Reactive] public bool IsLoadoutSelected { get; set; } = true;
}
