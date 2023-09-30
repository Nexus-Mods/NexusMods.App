using JetBrains.Annotations;

namespace NexusMods.Networking.NexusWebApi.Types;

/// <summary>
/// Information about a logged in user
/// </summary>
[PublicAPI]
public record UserInfo
{
    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the premium status of the user.
    /// </summary>
    public required bool IsPremium { get; init; }

    /// <summary>
    /// Gets the avatar url of the user.
    /// </summary>
    public required Uri? AvatarUrl { get; init; }
}
