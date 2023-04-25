using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI;

namespace NexusMods.UI.Tests;

public class AVmTest<TVm> : AUiTest, IDisposable
where TVm : IViewModelInterface
{
    private VMWrapper<TVm> _vmWrapper { get; }

    public AVmTest(IServiceProvider provider) : base(provider)
    {
        _vmWrapper = GetActivatedViewModel<TVm>();
    }

    public TVm Vm => _vmWrapper.VM;

    public void Dispose()
    {
        _vmWrapper.Dispose();
    }
}
