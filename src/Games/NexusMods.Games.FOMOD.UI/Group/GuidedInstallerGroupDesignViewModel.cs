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
