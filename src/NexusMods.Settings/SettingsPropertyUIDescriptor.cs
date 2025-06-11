using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal record SettingsPropertyUIDescriptor : ISettingsPropertyUIDescriptor
{
    public required SectionId SectionId { get; init; }
    public required string DisplayName { get; init; }
    public required Func<object, string> DescriptionFactory { get; init; }
    public required Uri? Link { get; init; }
    public required bool RequiresRestart { get; init; }
    public required string? RestartMessage { get; init; }
    public required SettingsPropertyValueContainer SettingsPropertyValueContainer { get; init; }

    public static SettingsPropertyUIDescriptor From(IPropertyBuilderOutput output, SettingsPropertyValueContainer valueContainer)
    {
        return new SettingsPropertyUIDescriptor
        {
            SectionId = output.SectionId,
            DisplayName = output.DisplayName,
            DescriptionFactory = output.DescriptionFactory,
            Link = output.Link,
            RequiresRestart = output.RequiresRestart,
            RestartMessage = output.RestartMessage,
            SettingsPropertyValueContainer = valueContainer,
        };
    }
}
