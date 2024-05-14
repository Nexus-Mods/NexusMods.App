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
            using var tx = Db.Connection.BeginTransaction();
            Delete(tx);
            await tx.Commit();
        }

        /// <summary>
        /// Adds a retraction which deletes the current mod and its files from the loadout.
        /// </summary>
        /// <param name="tx">The transaction to add the retraction to.</param>
        public void Delete(ITransaction tx)
        {
            foreach (var file in this.Files)
                tx.Retract(file.Id, File.Loadout, this.Loadout.Id);

            tx.Retract(this.Id, Mod.Loadout, this.Loadout.Id);
            this.Revise(tx);
        }
    }
}
