using NexusMods.Abstractions.Settings;

namespace NexusMods.Settings;

internal record SettingsPropertyUIDescriptor : ISettingsPropertyUIDescriptor
{
    public required SectionId SectionId { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required bool RequiresRestart { get; init; }
    public required string? RestartMessage { get; init; }
    public required SettingsPropertyValueContainer SettingsPropertyValueContainer { get; init; }

    public static SettingsPropertyUIDescriptor From(PropertyBuilderOutput output, SettingsPropertyValueContainer valueContainer)
    {
        return new SettingsPropertyUIDescriptor
        {
            SectionId = output.SectionId,
            DisplayName = output.DisplayName,
            Description = output.Description,
            RequiresRestart = output.RequiresRestart,
            RestartMessage = output.RestartMessage,
            SettingsPropertyValueContainer = valueContainer,
        };
    }
}
