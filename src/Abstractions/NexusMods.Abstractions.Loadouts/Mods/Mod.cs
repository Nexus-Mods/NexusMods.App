using System.Reactive.Linq;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Mods;

/// <summary>
/// Represents an individual mod recognised by NMA.
/// Please see remarks for current details.
/// </summary>
/// <remarks>
///    At the current moment in time [8th of March 2023]; represents
///    *an installed mod from an archive*, i.e. only archives are supported
///    at the moment and files are pushed out to game directory.<br/><br/>
///
///    This will change some time in the future.
/// </remarks>
public partial class Mod : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Mods.Mod";

    /// <summary>
    /// Name of the mod in question.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// Revision number of the mod.
    /// </summary>
    public static readonly ULongAttribute Revision = new(Namespace, nameof(Revision));

    /// <summary>
    /// The version of the mod.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version)) { IsOptional = true };
    
    /// <summary>
    /// The loadout this mod is part of.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// The Download Metadata the mod was installed from.
    /// </summary>
    public static readonly ReferenceAttribute<DownloadAnalysis> Source = new(Namespace, nameof(Source)) { IsOptional = true };
    
    /// <summary>
    /// The enabled status of the mod
    /// </summary>
    public static readonly BooleanAttribute Enabled = new(Namespace, nameof(Enabled));
    
    /// <summary>
    /// The install status of the mod.
    /// </summary>
    public static readonly EnumAttribute<ModStatus> Status = new(Namespace, nameof(Status));
    
    /// <summary>
    /// The category of the mod.
    /// </summary>
    public static readonly EnumAttribute<ModCategory> Category = new(Namespace, nameof(Category)) { IsIndexed = true };


    /// <summary>
    /// Sort this mod after another mod, mostly used as a placeholder until we figure out better
    /// sorting mechanisms.
    /// </summary>
    public static readonly ReferenceAttribute<Mod> SortAfter = new(Namespace, nameof(SortAfter)) { IsOptional = true };
    
    /// <summary>
    /// The files that are part of this mod.
    /// </summary>
    public static readonly BackReferenceAttribute<File> Files = new(File.Mod);
    
    public partial struct ReadOnly 
    {
        /// <summary>
        /// Issue a new revision of this loadout into the transaction, this will increment the revision number,
        /// and also revise the loadout this mod is part of.
        /// </summary>
        public void Revise(ITransaction tx)
        {
            tx.Add(Id, static (innerTx, db, id) =>
            {
                var self = new ReadOnly(db, id);
                innerTx.Add(id, Mod.Revision, self.Revision + 1);
            });
            Loadout.Revise(tx);
        }

        /// <summary>
        /// Returns the timestamp of the transaction that created this mod.
        /// </summary>
        public DateTime CreatedAt
        {
            get
            {
                var lowestTx = this.Min(d => d.T);
                var tx = Transaction.Load(Db, EntityId.From(lowestTx.Value));
                return tx.Timestamp;
            }
        }

    }
}
