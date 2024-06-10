using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Helper extensions for models
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// The most recent transaction Id that modified this model
    /// </summary>
    public static TxId MostRecentTx(this IReadOnlyModel model) => model.Max(d => d.T);
    

}
