using JetBrains.Annotations;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Information about a logged-in user
/// </summary>
[PublicAPI]
public record UserInfo
{
    /// <summary>
    /// Gets the ID of the user.
    /// </summary>
    public required UserId UserId { get; init; }

    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the role of the user.
    /// </summary>
    public required UserRole UserRole { get; init; }

    /// <summary>
    /// Gets the avatar url of the user.
    /// </summary>
    public required Uri? AvatarUrl { get; init; }
}
