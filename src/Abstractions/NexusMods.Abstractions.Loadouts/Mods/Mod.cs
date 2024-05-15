using System.Reactive.Linq;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;
using Entity = NexusMods.MnemonicDB.Abstractions.Models.Entity;
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
public static partial class Mod
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
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));
    
    /// <summary>
    /// The loadout this mod is part of.
    /// </summary>
    public static readonly ReferenceAttribute Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// The Download Metadata the mod was installed from.
    /// </summary>
    public static readonly ReferenceAttribute Source = new(Namespace, nameof(Source));
    
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
    public static readonly ReferenceAttribute SortAfter = new(Namespace, nameof(SortAfter));
    

    /// <summary>
    /// Gets all the revisions of a loadout over time
    /// </summary>
    public static IObservable<Model> Revisions(this IConnection conn, ModId id)
    {
        // All db revisions that contain the loadout id, select the loadout
        return conn.Revisions
            .Where(db => db.Datoms(db.BasisTxId).Any(datom => datom.E == id.Value))
            .StartWith(conn.Db)
            .Select(db => db.Get<Model>(id.Value));
    }


    public partial class Model(ITransaction tx) : Entity(tx)
    {
        
        /// <summary>
        /// Remaps the entity id into a mod id, this is mostly just a cast.
        /// </summary>
        public ModId ModId => ModId.From(Id);
        
        public string Name
        {
            get => Mod.Name.Get(this);
            set => Mod.Name.Add(this, value);
        }
        
        /// <summary>
        /// The revision number for this mod, increments by one for each change.
        /// </summary>
        public ulong Revision
        {
            get => Mod.Revision.Get(this, 0);
            set => Mod.Revision.Add(this, value);
        }
        
        public string Version
        {
            get => Mod.Version.Get(this, "<unknown>");
            set => Mod.Version.Add(this, value);
        }
        
        public bool Enabled
        {
            get => Mod.Enabled.Get(this);
            set => Mod.Enabled.Add(this, value);
        }
        
        public ModStatus Status
        {
            get => Mod.Status.Get(this, ModStatus.Installed);
            set => Mod.Status.Add(this, value);
        }
        
        public LoadoutId LoadoutId
        {
            get => LoadoutId.From(Mod.Loadout.Get(this));
            set => Mod.Loadout.Add(this, value.Value);
        }

        public Loadout.Model Loadout
        {
            get => Db.Get<Loadout.Model>(LoadoutId.Value);
            set => Mod.Loadout.Add(this, value.Id);
        }
        
        public EntityId SourceId
        {
            get => Mod.Source.Get(this);
            set => Mod.Source.Add(this, value);
        }
        
        public DownloadAnalysis.Model Source
        {
            get => Db.Get<DownloadAnalysis.Model>(SourceId);
            set => Mod.Source.Add(this, value.Id);
        }

        public ModCategory Category
        {
            get => Mod.Category.Get(this);
            set => Mod.Category.Add(this, value);
        }

        /// <summary>
        /// The timestamp of the creation of this mod.
        /// </summary>
        public DateTime GetCreatedAt()
        {
            // Get the lowest transaction id, then get the timestamp of that transaction
            var t = this.Select(d => d.T).Min();
            var txEntity = Db.Get<Entity>(EntityId.From(t.Value));
            return BuiltInAttributes.TxTimestanp.Get(txEntity);
        }

        public Entities<EntityIds, File.Model> Files => GetReverse<File.Model>(File.Mod);
        
        
        /// <summary>
        /// Issue a new revision of this loadout into the transaction, this will increment the revision number,
        /// and also revise the loadout this mod is part of.
        /// </summary>
        public void Revise(ITransaction tx)
        {
            tx.Add(Id, static (innerTx, db, id) =>
            {
                var self = db.Get<Model>(id);
                innerTx.Add(id, Mod.Revision, self.Revision + 1);
            });
            Loadout.Revise(tx);
        }
        
    }
}
