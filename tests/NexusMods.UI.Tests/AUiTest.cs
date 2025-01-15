using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.UI;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class AUiTest
{
    protected IServiceProvider Provider;

    protected IConnection Connection { get; }
    protected AvaloniaApp App { get; }

    public AUiTest(IServiceProvider provider)
    {
        Provider = provider;

        // Do this to trigger the AvaloniaApp constructor/initialization
        App = provider.GetRequiredService<AvaloniaApp>();
        Connection = provider.GetRequiredService<IConnection>();
    }

    protected VMWrapper<T> GetActivatedViewModel<T>()
    where T : IViewModelInterface
    {
        var vm = Provider.GetRequiredService<T>();
        return new VMWrapper<T>(vm);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class VMWrapper<T> : IDisposable where T : IViewModelInterface
    {
        private readonly IDisposable _disposable;
        public T VM { get; }
        public VMWrapper(T vm)
        {
            VM = vm;
            _disposable = vm.Activator.Activate();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public void Deconstruct(out T vm)
        {
            vm = VM;
        }
    }


    /// <summary>
    /// Retries the action until it succeeds or the timeout is reached. If the timeout is reached, the last exception is
    /// rethrown.
    /// </summary>
    /// <param name="action">The code to run on each attempt</param>
    /// <param name="maxTimeout">The maximum amount of time to wait for a successful completion</param>
    /// <param name="delay">Amount of time to delay between each attempt</param>
    public async Task Eventually(Action action, TimeSpan? maxTimeout = null, TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromMilliseconds(500);
        maxTimeout ??= TimeSpan.FromSeconds(15);
        var sw = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception)
            {
                if (sw.Elapsed >= maxTimeout)
                    throw;
                await Task.Delay(delay.Value).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Retries the action until it succeeds or the timeout is reached. If the timeout is reached, the last exception is
    /// rethrown.
    /// </summary>
    /// <param name="action">The code to run on each attempt</param>
    /// <param name="maxTimeout">The maximum amount of time to wait for a successful completion</param>
    /// <param name="delay">Amount of time to delay between each attempt</param>
    public async Task Eventually(Func<Task> action, TimeSpan? maxTimeout = null, TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromMilliseconds(500);
        maxTimeout ??= TimeSpan.FromSeconds(15);
        var sw = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception)
            {
                if (sw.Elapsed >= maxTimeout)
                    throw;
                await Task.Delay(delay.Value).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Like Eventually, but runs the action on the UI thread.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="maxTimeout"></param>
    /// <param name="delay"></param>
    public async Task EventuallyOnUi(Func<Task> action, TimeSpan? maxTimeout = null, TimeSpan? delay = null)
    {
        await Eventually(async () =>
        {
            await OnUi(async () =>
            {
                await action();
            });
        });
    }

    /// <summary>
    /// Like Eventually, but runs the action on the UI thread.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="maxTimeout"></param>
    /// <param name="delay"></param>
    public async Task EventuallyOnUi(Action action, TimeSpan? maxTimeout = null, TimeSpan? delay = null)
    {
        await Eventually(async () => await OnUi(action));
    }

    /// <summary>
    /// Executes an action on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action"></param>
    protected async Task<T> OnUi<T>(Func<Task<T>> action)
    {
        var result = await Dispatcher.UIThread.InvokeAsync(action);
        return result;
    }

    /// <summary>
    /// Executes an action on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action"></param>
    protected async Task OnUi(Func<Task> action)
    {
        var result = await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await action();
            return 0;
        });
    }

    /// <summary>
    /// Executes an action on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action"></param>
    protected async Task OnUi(Action action)
    {
        var result = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            action();
            return 0;
        });
    }

    /// <summary>
    /// Clicks the button in a way that fires all the proper UI events
    /// </summary>
    /// <param name="button"></param>
    protected async Task Click(Button button)
    {
        await OnUi(() => Click_AlreadyOnUi(button));
    }

    /// <summary>
    /// Clicks the button in a way that fires all the proper UI events, use this if you are
    /// already on UI via <see cref="OnUi{T}"/>.
    /// </summary>
    /// <param name="button"></param>
    protected void Click_AlreadyOnUi(Button button)
    {
        var peer = new ButtonAutomationPeer(button);
        peer.Invoke();
    }
}
