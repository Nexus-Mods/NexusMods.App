using System.Reactive.Disposables;
using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts.Ids;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.DataModel.Synchronizer;

public class SynchronizerState : ReactiveObject
{
    private bool _busy = false;
    private Percent _progress = Percent.Zero;

    public bool Busy
    {
        get => _busy;
        set => this.RaiseAndSetIfChanged(ref _busy, value);
    }

    public Percent Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

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
