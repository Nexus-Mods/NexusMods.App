using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using NexusMods.App;
using NexusMods.App.UI;
using NexusMods.Common;
using NexusMods.Paths;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using ReactiveUI;
using Splat;

namespace NexusMods.UI.Tests;

public class AppHelper : IDisposable
{
    private readonly AppBuilder _app;
    private bool _disposed;
    private readonly IServiceProvider _serviceProvider;

    public AppHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var tcs = new TaskCompletionSource<SynchronizationContext>();
        var thread = new Thread(() =>
        {
            try
            {
                var builder = AppBuilder.Configure(serviceProvider.GetRequiredService<App.UI.App>)
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .UseSkia()
                    .LogToTrace()
                    .UseReactiveUI()
                    .WithIcons(c => c.Register<MaterialDesignIconProvider>());

                Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
                Locator.CurrentMutable.Register(serviceProvider.GetRequiredService<InjectedViewLocator>, typeof(IViewLocator));

                    builder.AfterSetup(_ =>
                    {
                        tcs.SetResult(SynchronizationContext.Current);
                    })
                    .StartWithClassicDesktopLifetime(Array.Empty<string>());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        SynchronizationContext.SetSynchronizationContext(tcs.Task.Result);

        while (true)
        {
            var lifetime = ApplicationLifetime;

            if (lifetime?.MainWindow != null) break;
            Thread.Sleep(100);
        }
    }

    private static IClassicDesktopStyleApplicationLifetime? ApplicationLifetime =>
        Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    public void Dispose()
    {
        ApplicationLifetime!.Shutdown();
    }

    public async Task<HostedControl<TView, TVm>> MakeHost<TView, TVm>()
        where TView : IViewFor<TVm> where TVm : class, IViewModelInterface
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var vm = _serviceProvider
                .GetRequiredService<HostWindowViewModel>();
            vm.Content = _serviceProvider.GetRequiredService<TVm>();
            var window = new HostWindow
            {
                ViewModel = vm
            };
            window.Show();
            return new HostedControl<TView, TVm>()
            {
                ViewModel = (TVm)vm.Content,
                Window = window
            };
        });
    }
}
