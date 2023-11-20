using TransparentValueObjects;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Name of the CDN server that handles your download request.
/// </summary>
[ValueObject<string>]
// ReSharper disable once InconsistentNaming
public readonly partial struct CDNName : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static CDNName DefaultValue => From(string.Empty);
}
