using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.UI.Tests.Framework;

public class ControlHost<TView, TVm, TInterface> : IAsyncDisposable
    where TView : ReactiveUserControl<TInterface>
    where TInterface : class, IViewModelInterface
    where TVm : AViewModel<TInterface> 
{
    public TView View { get; init; }
    public TVm ViewModel { get; init; }
    public Window Window { get; init; }
    public AvaloniaApp App { get; init; }


    public async ValueTask DisposeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // If we close here, the app hangs when using the headless renderer.
            // so either we can use the platform renderer (and have flashing windows during testing)
            // or we hide the window instead :|
            Window.Hide();
        });
    }
    
    public async Task OnUi(Func<Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
        await Flush();
    }
    public async Task Flush()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
    
    public async Task<T> GetViewControl<T>(string launchbutton) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var btn = View.GetControl<T>(launchbutton);
            return btn;
        });
    }
}
