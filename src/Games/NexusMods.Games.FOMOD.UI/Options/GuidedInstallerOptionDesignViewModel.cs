using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionDesignViewModel : GuidedInstallerOptionViewModel
{
    public GuidedInstallerOptionDesignViewModel() : base(GenerateOption()) { }

    public GuidedInstallerOptionDesignViewModel(Option option) : base(option) { }

    private static Option GenerateOption()
    {
        return new Option
        {
            Id = OptionId.From(Guid.NewGuid()),
            Name = "Test Option",
            Type = OptionType.Available,
            Description = "An option",
            HoverText = "This is some hover text"
        };
    }
}
