using Avalonia.Threading;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.UI.Tests;

public class HostedControl<TView, TVm>: IAsyncDisposable where TView : IViewFor<TVm> where TVm : class, IViewModelInterface {

    public HostWindow Window { get; init; }
    public HostWindowViewModel WindowViewModel { get; init; }
    public TView View { get; init; }
    public TVm ViewModel { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { Window.Close();});
    }
}
