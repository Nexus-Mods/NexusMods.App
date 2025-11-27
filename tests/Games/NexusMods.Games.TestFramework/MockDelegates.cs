using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace NexusMods.Games.TestFramework;

public class MockDelegates : ICoreDelegates
{
    public IContextDelegates context => throw new NotImplementedException();
    public IIniDelegates ini => throw new NotImplementedException();
    public IPluginDelegates plugin => throw new NotImplementedException();
    public IUIDelegates ui => throw new NotImplementedException();
}
