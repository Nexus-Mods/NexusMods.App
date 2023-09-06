using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionViewModel : AViewModel<IGuidedInstallerOptionViewModel>, IGuidedInstallerOptionViewModel
{
    public Option Option { get; }

    [Reactive]
    public bool IsEnabled { get; set; }

    [Reactive]
    public bool IsSelected { get; set; }

    [Reactive]
    public bool IsHighlighted { get; set; }

    public ReactiveCommand<Unit, Unit> OptionPressed { get; }

    public GuidedInstallerOptionViewModel(Option option)
    {
        Option = option;
        IsEnabled = option.Type is not OptionType.Disabled and not OptionType.Required;
        IsSelected = option.Type is OptionType.PreSelected or OptionType.Required;

        OptionPressed = ReactiveCommand.Create(() =>
        {
            IsHighlighted = !IsHighlighted;
        });
    }
}
