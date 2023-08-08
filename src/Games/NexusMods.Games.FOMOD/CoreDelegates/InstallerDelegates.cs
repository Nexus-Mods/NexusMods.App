using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Common.UserInput;

namespace NexusMods.Games.FOMOD.CoreDelegates;

[PublicAPI]
public sealed class InstallerDelegates : ICoreDelegates
{
    public IContextDelegates context => throw new NotImplementedException();
    public IIniDelegates ini => throw new NotImplementedException();
    public IPluginDelegates plugin => throw new NotImplementedException();
    public IUIDelegates ui { get; }

    public InstallerDelegates(ILoggerFactory loggerFactory, IOptionSelector optionSelector)
    {
        ui = new UiDelegates(loggerFactory.CreateLogger<UiDelegates>(), optionSelector);
    }
}
