using NexusMods.Abstractions.Settings;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.Settings;

internal record SettingsSectionDescriptor : ISettingsSectionDescriptor
{
    public required SectionId Id { get; init; }

    public required string Name { get; init; }

    public required IconValue Icon { get; init; }

    public required ushort Priority { get; init; }

    public required bool Hidden { get; init; }
}
