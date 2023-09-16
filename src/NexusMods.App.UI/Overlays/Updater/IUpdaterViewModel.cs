using System.Windows.Input;
using NexusMods.Common;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Updater;

public interface IUpdaterViewModel : IOverlayViewModel
{
    public InstallationMethod Method { get; }
    public Version NewVersion { get; }
    public Version OldVersion { get; }

    public ICommand UpdateCommand { get; }

    public bool ShowSystemUpdateMessage { get; }
}
