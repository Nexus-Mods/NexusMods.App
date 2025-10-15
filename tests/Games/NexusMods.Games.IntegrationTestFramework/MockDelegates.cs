using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Games.IntegrationTestFramework;

public class MockDelegates : ICoreDelegates
{
    public IPluginDelegates plugin => throw new NotSupportedException();

    public IContextDelegates context => throw new NotImplementedException();

    public IIniDelegates ini => throw new NotImplementedException();

    public IUIDelegates ui => throw new NotImplementedException();

    public MockDelegates(IServiceProvider provider)
    {
        //ui = ActivatorUtilities.CreateInstance<UIDelegates>(provider);
    }
}
