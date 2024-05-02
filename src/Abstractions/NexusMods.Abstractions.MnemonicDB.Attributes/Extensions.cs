using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Extension methods for MnemonicDB.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Casts an entity to a specific type, performing no checks.
    /// </summary>
    public static T Remap<T>(this Entity entity) where T : Entity
    {
        return entity.Db.Get<T>(entity.Id);
    }
    
    
    /// <summary>
    /// Tries to parse an entity id from a hex string.
    /// </summary>
    public static bool TryParseFromHex(string hex, out EntityId id)
    {
        if (ulong.TryParse(hex, out var parsed))
        {
            id = EntityId.From(parsed);
            return true;
        }

        id = default;
        return false;
    }
}
