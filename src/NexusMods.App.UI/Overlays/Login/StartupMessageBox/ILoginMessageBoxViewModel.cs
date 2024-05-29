using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Login;

public interface ILoginMessageBoxViewModel : IOverlayViewModel
{
    ReactiveCommand<Unit,Unit> OkCommand { get; }
    ReactiveCommand<Unit,Unit> CancelCommand { get; }

    bool MaybeShow();
}
