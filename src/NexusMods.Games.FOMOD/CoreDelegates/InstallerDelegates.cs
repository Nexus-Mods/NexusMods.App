using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GuidedInstallers;

namespace NexusMods.Games.FOMOD.CoreDelegates;

[UsedImplicitly]
public sealed class InstallerDelegates : ICoreDelegates
{
    public IContextDelegates context { get; }
    public IIniDelegates ini => throw new NotImplementedException();
    public IPluginDelegates plugin { get; }

    public IUIDelegates ui => UiDelegates;
    public UiDelegates UiDelegates;

    public InstallerDelegates(
        ILoggerFactory loggerFactory,
        IGuidedInstaller guidedInstaller)
    {
        context = new ContextDelegates(loggerFactory.CreateLogger<ContextDelegates>());
        plugin = new PluginDelegates(loggerFactory.CreateLogger<PluginDelegates>());
        UiDelegates = new UiDelegates(
            loggerFactory.CreateLogger<UiDelegates>(),
            guidedInstaller
        );
    }
}
