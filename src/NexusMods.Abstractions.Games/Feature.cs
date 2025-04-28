using JetBrains.Annotations;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents a feature a game can support.
/// </summary>
/// <param name="Description">Description</param>
[PublicAPI]
public readonly record struct Feature(string Description)
{
    /// <summary>
    /// Identifier.
    /// </summary>
    public readonly Guid Id = Guid.NewGuid();

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Equality.
    /// </summary>
    public bool Equals(Feature? other) => other is not null && Id.Equals(other.Value.Id);
}

/// <summary>
/// Status of a feature.
/// </summary>
/// <param name="Feature">The feature.</param>
/// <param name="IsImplemented">Whether the feature is implemented or not.</param>
[PublicAPI]
public readonly record struct FeatureStatus(Feature Feature, bool IsImplemented);

/// <summary>
/// Status of all game features.
/// </summary>
[PublicAPI]
public enum GameFeatureStatus
{
    /// <summary>
    /// Default value.
    /// </summary>
    None = 0,

    /// <summary>
    /// The minimum amount of features is implemented.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// All features are implemented.
    /// </summary>
    Full = 2,
}

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class FeatureExtensions
{
    /// <summary>
    ///
    /// </summary>
    public static GameFeatureStatus ToStatus(this HashSet<FeatureStatus> features)
    {
        var implemented = features.Count(status => status.IsImplemented);
        var total = features.Count;
        if (implemented == total) return GameFeatureStatus.Full;

        return implemented switch
        {
            0 => GameFeatureStatus.None,
            _ => GameFeatureStatus.Minimal,
        };
    }
}
