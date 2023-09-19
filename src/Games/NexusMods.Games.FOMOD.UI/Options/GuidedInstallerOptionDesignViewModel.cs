using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerOptionDesignViewModel : GuidedInstallerOptionViewModel
{
    public GuidedInstallerOptionDesignViewModel() : base(GenerateOption(), GenerateGroup()) { }

    public GuidedInstallerOptionDesignViewModel(Option option, OptionGroup group) : base(option, group) { }

    private static OptionGroup GenerateGroup()
    {
        return new OptionGroup
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = "Group",
            Options = Array.Empty<Option>(),
            Type = OptionGroupType.Any,
        };
    }

    private static Option GenerateOption()
    {
        return new Option
        {
            Id = OptionId.From(Guid.NewGuid()),
            Name = "Test Option",
            Type = OptionType.Available,
            Description = "An option",
            Image = new OptionImage(new Uri("https://http.cat/images/418.jpg")),
            HoverText = "This is some hover text"
        };
    }
}
