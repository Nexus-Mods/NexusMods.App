using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace NexusMods.Games.FOMOD;

public class InstallerDelegates : ICoreDelegates
{
    public IPluginDelegates plugin => throw new NotImplementedException();

    public IContextDelegates context => throw new NotImplementedException();

    public IIniDelegates ini => throw new NotImplementedException();

    public IUIDelegates ui => throw new NotImplementedException();
}
