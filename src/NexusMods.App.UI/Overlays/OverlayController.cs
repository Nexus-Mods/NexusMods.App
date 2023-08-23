﻿using System.Reactive.Subjects;
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

/// <inheritdoc />
public class OverlayController : IOverlayController
{
    /// <summary>
    /// The current stack of overlays. The overlays
    /// </summary>
    private Queue<SetOverlayItem> Overlays { get; } = new();

    private SetOverlayItem? _currentOverlayViewModel;
    private readonly Subject<SetOverlayItem?> _started = new();

    /// <inheritdoc />
    public IObservable<SetOverlayItem?> ApplyNextOverlay => _started;

    /// <inheritdoc />
    public SetOverlayItem? GetLastOverlay()
    {
        return _currentOverlayViewModel;
    }

    private void QueueOrSetOverlayViewModel(SetOverlayItem vm)
    {
        if (_currentOverlayViewModel != null)
            Overlays.Enqueue(vm);
        else
        {
            SetOverlayViewModel(vm);
        }
    }

    private void SetOverlayViewModel(SetOverlayItem? vm)
    {
        _started.OnNext(vm);
        _currentOverlayViewModel = vm;
    }

    /// <inheritdoc />
    public async Task<bool> ShowCancelDownloadOverlay(IDownloadTaskViewModel task, object? referenceItem = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var vm = new CancelDownloadOverlayViewModel(task);
        SetOverlayContent(new SetOverlayItem(vm), tcs);
        await tcs.Task;
        return vm.DialogResult;
    }

    /// <summary>
    /// Sets the overlay to display to the screen.
    /// </summary>
    /// <param name="item">The item being signaled.</param>
    /// <param name="tcs">Signals completion. This field is optional for tests only.</param>
    public void SetOverlayContent(SetOverlayItem item, TaskCompletionSource<bool>? tcs = null)
    {
        // Make sure IsActive is true, if it's initialized to false it breaks the overlay.
        item.VM.IsActive = true;

        // Register overlay close.
        var unsubscribeToken = new CancellationTokenSource();

        // Note(Sewer): This throws a warning because vm is a POCO that doesn't emit change notification.
        // This is however irrelevant because VM is member of record, it is readonly; the element inside successfully emits
        // change notifications. I just don't know how to suppress warning here from WhenAnyValue
        item.WhenAnyValue(x => x.VM.IsActive)
            .OnUI()
            .Subscribe(b =>
            {
                if (b)
                    return;

                // On unsubscribe (IsActive == false), pop next overlay from stack.
                SetOverlayViewModel(null);
                if (Overlays.TryDequeue(out var result))
                    QueueOrSetOverlayViewModel(result);

                unsubscribeToken.Cancel();

                // Complete the task if it exists.
                tcs?.TrySetResult(true);
            }, unsubscribeToken.Token);

        // Set the new overlay.
        QueueOrSetOverlayViewModel(item);
    }
}
