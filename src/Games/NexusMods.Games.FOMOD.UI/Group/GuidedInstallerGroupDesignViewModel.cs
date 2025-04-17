using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerGroupDesignViewModel : GuidedInstallerGroupViewModel
{
    public GuidedInstallerGroupDesignViewModel() : base(SetupGroup()) { }

    private static OptionGroup SetupGroup()
    {
        return new OptionGroup
        {
            Id = GroupId.From(Guid.NewGuid()),
            Name = "Test Group",
            Type = OptionGroupType.ExactlyOne,
            Options = GenerateAllOptionTypes()
        };
    }

    internal static Option[] GenerateOptions(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var description = Random.Shared.Next(0, 100) > 50 ? null : $"This is option {i + 1}";
                var image = Random.Shared.Next(0, 100) > 50 ? null : new OptionImage(new Uri("https://http.cat/images/418.jpg"));

                return new Option
                {
                    Id = OptionId.From(Guid.NewGuid()),
                    Name = $"Option {i + 1}",
                    Type = OptionType.Available,
                    Description = description,
                    Image = image,
                };
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
                Description = "This option is disabled",
                Image = new OptionImage(new Uri("https://http.cat/images/418.jpg")),
            },
        };
    }
}
