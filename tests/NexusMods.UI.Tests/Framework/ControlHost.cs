using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using NexusMods.Abstractions.UI;

namespace NexusMods.UI.Tests.Framework;

/// <summary>
/// A container for a viewmodel and a view and the window that hosts them.
/// </summary>
/// <typeparam name="TView"></typeparam>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TInterface"></typeparam>
public class ControlHost<TView, TVm, TInterface> : IAsyncDisposable
    where TView : ReactiveUserControl<TInterface>
    where TInterface : class, IViewModelInterface
    where TVm : TInterface
{
    private readonly TView? _view;
    private readonly TVm? _viewModel;
    private readonly Window? _window;
    private readonly AvaloniaApp? _app;

    /// <summary>
    /// The view control that is being tested.
    /// </summary>
    public TView View
    {
        get => _view!;
        init => _view = value;
    }

    /// <summary>
    /// The view model backing the view
    /// </summary>
    public TVm ViewModel
    {
        get => _viewModel!;
        init => _viewModel = value;
    }

    /// <summary>
    /// The window that hosts the view
    /// </summary>
    public Window Window
    {
        get => _window!;
        init => _window = value;
    }

    /// <summary>
    /// The app that hosts the window
    /// </summary>
    public AvaloniaApp App
    {
        get => _app!;
        init => _app = value;
    }


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

    /// <summary>
    /// Searches for a control of type T with the given name in the view.
    /// </summary>
    /// <param name="launchbutton"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetViewControl<T>(string launchbutton) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var btn = View.GetControl<T>(launchbutton);
            return btn;
        });
    }
}
