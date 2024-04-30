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
    public static T As<T>(this Entity entity) where T : Entity
    {
        return entity.Db.Get<T>(entity.Id);
    }
}
