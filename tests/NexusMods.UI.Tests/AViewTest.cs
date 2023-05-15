using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.UI.Tests.Framework;
using ReactiveUI;

namespace NexusMods.UI.Tests;

public class AViewTest<TView, TViewModel, TViewModelInterface> : AUiTest, IAsyncLifetime 
    where TViewModelInterface : class, IViewModelInterface 
    where TViewModel : TViewModelInterface, new() 
    where TView : ReactiveUserControl<TViewModelInterface>, new()
{
    private readonly AvaloniaApp _app;
    private ControlHost<TView,TViewModel,TViewModelInterface>? _host;
    
    
    protected ControlHost<TView,TViewModel,TViewModelInterface> Host => _host!;
    protected TViewModel ViewModel => _host!.ViewModel;
    protected TView View => _host!.View;

    protected AViewTest(IServiceProvider provider, AvaloniaApp app) : base(provider)
    {
        _app = app;
    }
    
    protected async Task<T> GetControl<T>(string name) where T : Control
    {
        return await _host!.GetViewControl<T>(name);
    }


    public async Task InitializeAsync()
    {
        _host = await _app.GetControl<TView, TViewModel, TViewModelInterface>();
        await PostInitializeSetup();
    }

    protected virtual Task PostInitializeSetup()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_host != null) 
            await _host.DisposeAsync();
    }
}
