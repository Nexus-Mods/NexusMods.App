using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupDesignViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public IGuidedInstallerOptionViewModel[] Options { get; set; }

    public GuidedInstallerGroupDesignViewModel() : this(SetupGroup()) { }

    public GuidedInstallerGroupDesignViewModel(OptionGroup group)
    {
        Group = group;

        Options = group.Options
            .Select(option => (IGuidedInstallerOptionViewModel)new GuidedInstallerOptionDesignViewModel(option))
            .ToArray();
    }

    private static OptionGroup SetupGroup()
    {
        return new OptionGroup
        {
            Id = GroupId.From(Guid.NewGuid()),
            Description = "Test Group",
            Type = OptionGroupType.Any,
            Options = new[]
            {
                new Option
                {
                    Id = OptionId.From(Guid.NewGuid()),
                    Name = "Option 1",
                    Type = OptionType.Available,
                    Description = "This is a description"
                }
            }
        };
    }
}
