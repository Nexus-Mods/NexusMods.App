using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.UI.Tests.Framework;
using Noggog;
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

    protected AViewTest(IServiceProvider provider) : base(provider)
    {
        _app = provider.GetRequiredService<AvaloniaApp>();
    }
    
    protected async Task<T> GetControl<T>(string name) where T : Control
    {
        return await _host!.GetViewControl<T>(name);
    }

    protected async Task<T[]> GetVisualDescendants<T>(Control parent) where T : Control
    {
        return await OnUi(async () =>
        {
            Control[] GetChildren(Control control, bool topLevel)
            {
                var children = control.GetVisualChildren()
                    .SelectMany(c => GetChildren((Control)c, false));
                if (!topLevel)
                    return children.StartWith(control).ToArray();
                return children.ToArray();
            }
            return GetChildren(parent, true).OfType<T>().ToArray();
        });
    }


    public virtual async Task InitializeAsync()
    {
        _host = await _app.GetControl<TView, TViewModel, TViewModelInterface>();
        await PostInitializeSetup();
    }

    protected virtual Task PostInitializeSetup()
    {
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (_host != null) 
            await _host.DisposeAsync();
    }
}
