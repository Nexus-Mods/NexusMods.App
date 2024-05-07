using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Mods;

public static partial class Mod
{

    public partial class Model
    {
        /// <summary>
        /// Toggle the enabled status of the mod.
        /// </summary>
        public async Task ToggleEnabled()
        {
            using var tx = Db.Connection.BeginTransaction();
            var old = Db.Get(ModId);
            tx.Add(ModId, static (tx, db, id) =>
                {
                    var old = db.Get(id);
                    tx.Add(id.Value, Mod.Enabled, !old.Enabled);
                }
            );
            old.Loadout.Revise(tx);
            
            old.Revise(tx);
            await tx.Commit();
        }
        
        /// <summary>
        /// Deletes a mod from the loadout, by unlinking it and all the files
        /// associated with it. 
        /// </summary>
        public async Task Delete()
        {
            var old = Db.Get(ModId);
            using var tx = Db.Connection.BeginTransaction();
            foreach (var file in old.Files)
            {
                tx.Retract(file.Id, File.Loadout, old.Loadout.Id);
            }
            tx.Retract(old.Id, Mod.Loadout, old.Loadout.Id);
            old.Revise(tx);
            await tx.Commit();
        }
        
        
        
    }
}
