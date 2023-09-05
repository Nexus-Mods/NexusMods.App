using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupDesignViewModel : AViewModel<IGuidedInstallerGroupViewModel>, IGuidedInstallerGroupViewModel
{
    public OptionGroup Group { get; }

    public GuidedInstallerGroupDesignViewModel()
    {
        Group = new OptionGroup
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

    public GuidedInstallerGroupDesignViewModel(OptionGroup group)
    {
        Group = group;
    }
}
