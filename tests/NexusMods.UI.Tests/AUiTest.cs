using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;

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
}
