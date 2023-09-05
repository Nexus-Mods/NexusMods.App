using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionDesignViewModel : AViewModel<IGuidedInstallerOptionViewModel>, IGuidedInstallerOptionViewModel
{
    public Option Option { get; }

    [Reactive]
    public bool IsSelected { get; set; }

    public GuidedInstallerOptionDesignViewModel() : this(GenerateOption()) { }

    public GuidedInstallerOptionDesignViewModel(Option option)
    {
        Option = option;
    }

    private static Option GenerateOption()
    {
        return new Option
        {
            Id = OptionId.From(Guid.NewGuid()),
            Name = "Test Option",
            Type = OptionType.Available,
            Description = "An option",
        };
    }
}
