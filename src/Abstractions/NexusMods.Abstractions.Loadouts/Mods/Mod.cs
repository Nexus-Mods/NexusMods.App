using System.Collections.Immutable;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
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
public static class Mod
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Mods.Mod";

    /// <summary>
    /// Name of the mod in question.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// The loadout this mod is part of.
    /// </summary>
    public static readonly ReferenceAttribute Loadout = new(Namespace, nameof(Loadout));
    
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
    public static readonly EnumAttribute<ModCategory> Category = new(Namespace, nameof(Category));
    


    public class Model(ITransaction tx) : Entity(tx)
    {
        
        public string Name
        {
            get => Mod.Name.Get(this);
            set => Mod.Name.Add(this, value);
        }
        
        public bool Enabled
        {
            get => Mod.Enabled.Get(this);
            set => Mod.Enabled.Add(this, value);
        }
        
        public ModStatus Status
        {
            get => Mod.Status.Get(this);
            set => Mod.Status.Add(this, value);
        }
        
        public EntityId LoadoutId
        {
            get => Mod.Loadout.Get(this);
            set => Mod.Loadout.Add(this, value);
        }

        public Loadout.Model Loadout
        {
            get => Db.Get<Loadout.Model>(LoadoutId);
            set => Mod.Loadout.Add(this, value.Id);
        }

        public ModCategory Category
        {
            get => Mod.Category.Get(this);
            set => Mod.Category.Add(this, value);
        }
        
        public Entities<EntityIds, File.Model> Files => GetReverse<File.Model>(File.Mod);
        
        
    }
}
