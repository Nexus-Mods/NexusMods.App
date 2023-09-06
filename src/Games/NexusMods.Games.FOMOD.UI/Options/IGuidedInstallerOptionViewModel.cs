using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerOptionViewModel : IViewModel
{
    public Option Option { get; }

    public bool IsEnabled { get; set; }

    public bool IsSelected { get; set; }

    public bool IsHighlighted { get; set; }

    public ReactiveCommand<Unit, Unit> OptionPressed { get; }
}
