using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionViewModel : AViewModel<IGuidedInstallerOptionViewModel>, IGuidedInstallerOptionViewModel
{
    public Option Option { get; }
    public OptionGroup Group { get; }

    [Reactive] public bool IsEnabled { get; set; }
    [Reactive] public bool IsChecked { get; set; }
    [Reactive] public bool IsValid { get; set; } = true;

    public GuidedInstallerOptionViewModel(Option option, OptionGroup group)
    {
        Option = option;
        Group = group;
        IsEnabled = option.Type is not OptionType.Disabled and not OptionType.Required;
        IsChecked = option.Type is OptionType.PreSelected or OptionType.Required;
    }
}
