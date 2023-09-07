using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupDesignViewModel : GuidedInstallerGroupViewModel
{
    public GuidedInstallerGroupDesignViewModel() : this(SetupGroup()) { }

    public GuidedInstallerGroupDesignViewModel(OptionGroup group) : base(group, option => new GuidedInstallerOptionDesignViewModel(option)) { }

    private static OptionGroup SetupGroup()
    {
        return new OptionGroup
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = "Test Group",
            Type = OptionGroupType.Any,
            Options = GenerateAllOptionTypes()
        };
    }

    internal static Option[] GenerateOptions(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(i => new Option
            {
                Id = OptionId.From(Guid.NewGuid()),
                Name = $"Option {(i + 1)}",
                Type = OptionType.Available,
                Description = $"This is option {(i + 1)}"
            })
            .ToArray();
    }

    internal static Option[] GenerateAllOptionTypes()
    {
        return new[]
        {
            new Option
            {
                Id = OptionId.From(Guid.NewGuid()),
                Name = "Available Option",
                Type = OptionType.Available,
                Description = "This option is available",
            },
            new Option
            {
                Id = OptionId.From(Guid.NewGuid()),
                Name = "Pre-selected Option",
                Type = OptionType.PreSelected,
                Description = "This option is pre-selected"
            },
            new Option
            {
                Id = OptionId.From(Guid.NewGuid()),
                Name = "Required Option",
                Type = OptionType.Required,
                Description = "This option is required"
            },
            new Option
            {
                Id = OptionId.From(Guid.NewGuid()),
                Name = "Disabled Option",
                Type = OptionType.Disabled,
                Description = "This option is disabled"
            },
        };
    }
}
