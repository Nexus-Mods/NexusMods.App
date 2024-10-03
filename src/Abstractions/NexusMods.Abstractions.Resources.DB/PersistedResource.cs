using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Resources.DB;

[PublicAPI]
public partial class PersistedResource : IModelDefinition
{
    private const string Namespace = "NexusMods.Resources.PersistedResource";

    public static readonly BytesAttribute Data = new(Namespace, nameof(Data));

    public static readonly DateTimeAttribute ExpiresAt = new(Namespace, nameof(ExpiresAt));

    public static readonly HashAttribute ResourceIdentifierHash = new(Namespace, nameof(ResourceIdentifierHash));

    public partial struct ReadOnly
    {
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
