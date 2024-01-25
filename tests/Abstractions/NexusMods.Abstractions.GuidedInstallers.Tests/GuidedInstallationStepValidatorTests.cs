using FluentAssertions;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers.Tests;

public class GuidedInstallationStepValidatorTests
{

    [Theory]
    [InlineData(OptionGroupType.Any, new int[0], true)]
    [InlineData(OptionGroupType.Any, new[] { 0, 1 }, true)]
    [InlineData(OptionGroupType.ExactlyOne, new[] { 2 }, true)]
    [InlineData(OptionGroupType.ExactlyOne, new int[0], false)]
    [InlineData(OptionGroupType.ExactlyOne, new[] { 0, 1 }, false)]
    [InlineData(OptionGroupType.AtMostOne, new[] { 0 }, true)]
    [InlineData(OptionGroupType.AtMostOne, new int[0], true)]
    [InlineData(OptionGroupType.AtMostOne, new[] { 0, 1 }, false)]
    [InlineData(OptionGroupType.AtLeastOne, new[] { 0 }, true)]
    [InlineData(OptionGroupType.AtLeastOne, new[] { 0, 1 }, true)]
    [InlineData(OptionGroupType.AtLeastOne, new int[0], false)]
    public void IsValidGroupSelectionTest(OptionGroupType groupType, int[] selectionIndexes, bool isValid)
    {
        var installStep = CreateInstallationStep(groupType);
        var group = installStep.Groups.First();
        var selectedOptions = selectionIndexes.Select(index => new SelectedOption(group.Id, group.Options[index].Id)).ToList();

        // add a couple of extra options to the selected options, to simulate other group selections being present
        selectedOptions.AddRange(new[]
        {
            new SelectedOption(GroupId.From(new Guid("513C6470-639B-44E1-AB0B-DE410871E15D")), OptionId.From(new Guid("4D1F7EBE-010C-49DF-972B-91EEF093F432"))),
            new SelectedOption(GroupId.From(new Guid("513C6470-639B-44E1-AB0B-DE410871E15D")), OptionId.From(new Guid("23955466-1AA8-4339-8004-FA5A5F3EA10A")))
        });

        // test if validation works
        GuidedInstallerValidation.IsValidGroupSelection(group, selectedOptions).Should().Be(isValid);

        // test if the validation method works
        GuidedInstallerValidation.ValidateStepSelections(installStep, selectedOptions).Should().BeEquivalentTo(isValid ? Array.Empty<GroupId>() : new[] { group.Id });
    }

    private GuidedInstallationStep CreateInstallationStep(OptionGroupType groupType)
    {
        return new()
        {
            Id = StepId.From(new Guid("E0B0B0A0-0A0A-0A0A-0A0A-0A0A0A0A0A0A")),
            Name = "Step",
            Groups = new[]
            {
                CreateOptionGroup(groupType),
                new OptionGroup()
                {
                    Id = GroupId.From(new Guid("513C6470-639B-44E1-AB0B-DE410871E15D")),
                    Name = "Other Group",
                    Type = OptionGroupType.Any,
                    Options = new []
                    {
                        new Option
                        {
                            Id = OptionId.From(new Guid("4D1F7EBE-010C-49DF-972B-91EEF093F432")),
                            Name = "Some other option 0",
                            Description = "Some other option 0",
                            Type = OptionType.Available
                        },
                        new Option
                        {
                            Id = OptionId.From(new Guid("23955466-1AA8-4339-8004-FA5A5F3EA10A")),
                            Name = "Some other option 1",
                            Description = "Some other option 1",
                            Type = OptionType.Available
                        }
                    }
                }

            }
        };
    }
    private OptionGroup CreateOptionGroup(OptionGroupType groupType)
    {
        return new()
        {
            Id = GroupId.From(new Guid("54DDF485-124A-4E6E-B4B8-1A9B94533EC0")),
            Name = "Test group",
            Type = groupType,
            Options = new[]
            {
                new Option
                {
                    Id = OptionId.From(new Guid("C12F8029-4E73-4175-A0A3-8840DFA9416D")),
                    Name = "Test option 0",
                    Description = "Test option 0",
                    Type = OptionType.Available
                },
                new Option
                {
                    Id = OptionId.From(new Guid("6A2CAB8B-CFA3-4962-856B-36092E89914D")),
                    Name = "Test option 1",
                    Description = "Test option 1",
                    Type = OptionType.Available
                },
                new Option
                {
                    Id = OptionId.From(new Guid("1E59BD9A-9A88-4753-ADDE-07E625C911D8")),
                    Name = "Test option 2",
                    Description = "Test option 2",
                    Type = OptionType.Available
                },
                new Option
                {
                    Id = OptionId.From(new Guid("83BF090A-1D65-4CC7-BFAF-6157CBC986C6")),
                    Name = "Test option 3",
                    Description = "Test option 3",
                    Type = OptionType.Available
                }
            }
        };
    }
}
