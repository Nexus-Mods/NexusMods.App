using System.Reactive.Disposables;
using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts.Ids;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.DataModel.Synchronizer;

public class SynchronizerState
{
    [Reactive] public bool Busy { get; set; } = false;
    
    [Reactive] public Optional<LoadoutWithTxId> LastApplied { get; set; }

    /// <summary>
    /// Locks the state for the duration of the returned IDisposable. Will throw if already locked.
    /// </summary>
    public IDisposable WithLock()
    {
        lock (this)
        {
            if (Busy)
                throw new InvalidOperationException("State is already locked.");
            Busy = true;
            return Disposable.Create(() => Busy = false);
        }
    }
}
