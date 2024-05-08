using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// API Key storage for older Nexus Mods API calls. No model here because the
/// datamodel is so simple it's not worth it.
/// </summary>
public static class ApiKey
{
    private const string Namespace = "NexusMods.Networking.NexusWebApi.Auth.ApiKey";
    
    /// <summary>
    /// API Key storage for older Nexus Mods API calls
    /// </summary>
    public static readonly StringAttribute Key = new(Namespace, nameof(Key));

    /// <summary>
    /// Simple gett
    /// </summary>
    public static string? Get(IDb db)
    {
        var id = db.Find(Key).FirstOrDefault();
        return id != EntityId.From(0) ? Key.Get(db.Get<Entity>(id)) : null;
    }

    /// <summary>
    /// Set the key
    /// </summary>
    public static async Task Set(IConnection conn, string key)
    {
        using var tx = conn.BeginTransaction();
        var oldId = conn.Db.Find(Key).FirstOrDefault(tx.TempId());
        tx.Add(oldId, Key, key);
        await tx.Commit();
    }
}
