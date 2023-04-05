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

    public AppHelper(IServiceProvider serviceProvider)
    {
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
        Dispatcher.UIThread.Post(() => { ApplicationLifetime!.MainWindow!.Close();});
    }
}
