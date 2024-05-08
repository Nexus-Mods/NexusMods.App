using System.Reactive.Linq;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;

/// <summary>
/// Extension methods for the <see cref="IConnection"/> interface.
/// </summary>
public static class ConnectionExtensions
{
    
    /// <summary>
    /// Gets a pair of the database and the entity id for each revision of the given attribute, starts
    /// with the current revision of the database.
    /// </summary>
    public static IObservable<(IDb, EntityId)> UpdatesFor(this IConnection conn, IAttribute attribute)
    {
        var startingDb = conn.Db;
        return conn.Revisions
            .SelectMany(db =>
                {
                    var tx = db.Datoms(db.BasisTxId);
                    return tx.Where(d => d.A == attribute)
                        .Select(d => d.E)
                        .Distinct()
                        .Select(e => (db, e));
                }
            )
            .StartWith(startingDb.Find(attribute).Select(e => (startingDb, e)));
    }
}
