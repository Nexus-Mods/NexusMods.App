using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.MnemonicAttributes;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Represents a resource persisted in the database.
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
    public static readonly TimestampAttribute ExpiresAt = new(Namespace, nameof(ExpiresAt));

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
