using System.Globalization;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;

/// <summary>
/// Extension methods for MnemonicDB entities
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Gets the largest transaction id in the model.
    /// </summary>
    public static TxId MostRecentTxId(this IReadOnlyModel model)
    {
        return model.Max(m => m.T);
    }

    /// <summary>
    /// Gets the timestamp of the transaction that created the model.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dateTime">A default value to return if the model doesn't exist.</param>
    /// <returns></returns>
    public static DateTimeOffset GetCreatedAt<T>(this T model, DateTime? dateTime = null)
    where T : IReadOnlyModel
    {
        if (model.Count == 0)
            return dateTime ?? DateTime.MinValue;
        var tx = new Transaction.ReadOnly(model.Db, EntityId.From(model.Min(m => m.T).Value));
        return Transaction.Timestamp.Get(tx);
    }
    
    /// <summary>
    /// Tries to parse an entity id from a hex string.
    /// </summary>
    public static bool TryParseFromHex(string hex, out EntityId id)
    {
        var input = hex.AsSpan();
        if (input.StartsWith("EId:"))
            input = input[4..];
        
        if (ulong.TryParse(input, NumberStyles.HexNumber, null, out var parsed))
        {
            id = EntityId.From(parsed);
            return true;
        }

        id = default(EntityId);
        return false;
    }
}
