
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.GameRegistry;

public static class GameMetadata
{
    public const string Namespace = "NexusMods.DataModel.GameRegistry.GameMetadata";
    
    /// <summary>
    /// The game's domain.
    /// </summary>
    public static readonly StringAttribute Domain = new(Namespace, "Domain");
    
    /// <summary>
    /// The name of the store the game is from
    /// </summary>
    public static readonly StringAttribute Store = new(Namespace, "Store");
    
    /// <summary>
    /// Get all games from the database.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static IEnumerable<Model> All(IDb db)
    {
        return db.Find(Domain)
            .Select(db.Get<Model>);
    }
    
    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// The game's domain.
        /// </summary>
        public string Domain
        {
            get => GameMetadata.Domain.Get(this);
            set => GameMetadata.Domain.Add(this, value);
        }
        
        /// <summary>
        /// The name of the store the game is from
        /// </summary>
        public string Store
        {
            get => GameMetadata.Store.Get(this);
            set => GameMetadata.Store.Add(this, value);
        }
    }
}
