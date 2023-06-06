using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class AUiTest
{
    private readonly IServiceProvider _provider;

    public AUiTest(IServiceProvider provider)
    {
        _provider = provider;

        // Do this to trigger the AvaloniaApp constructor/initialization
        provider.GetRequiredService<AvaloniaApp>();
    }

    protected VMWrapper<T> GetActivatedViewModel<T>()
    where T : IViewModelInterface
    {
        var vm = _provider.GetRequiredService<T>();
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
                await Task.Delay(delay.Value);
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
                await Task.Delay(delay.Value);
            }
        }
    }
}
