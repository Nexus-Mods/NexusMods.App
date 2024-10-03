using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Resources.DB;

/// <summary>
/// Represents a resources persisted in the database.
/// </summary>
[PublicAPI]
public partial class PersistedDbResource : IModelDefinition
{
    private const string Namespace = "NexusMods.Resources.PersistedResource";

    /// <summary>
    /// The raw data.
    /// </summary>
    public static readonly BytesAttribute Data = new(Namespace, nameof(Data));

    /// <summary>
    /// The expiration date.
    /// </summary>
    public static readonly DateTimeAttribute ExpiresAt = new(Namespace, nameof(ExpiresAt));

    /// <summary>
    /// The resource identifier as a hash.
    /// </summary>
    public static readonly HashAttribute ResourceIdentifierHash = new(Namespace, nameof(ResourceIdentifierHash));

    public partial struct ReadOnly
    {
        /// <summary>
        /// Whether the resource is expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
