using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

[PublicAPI]
public record Option
{
    public required OptionId Id { get; init; }

    public required OptionType OptionType { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public AssetUrl? ImageUrl { get; init; }

    public string HoverText { get; init; } = string.Empty;
}
