using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Represents an individual file which belongs to a <see cref="Loadout"/>, all files
/// should at least have the <see cref="Loadout"/> reference, and optionally a reference to a <see cref="Mod"/>,
/// </summary>
public static class File
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Mods.ModFile";

    /// <summary>
    /// The loadout this file is part of.
    /// </summary>
    public static readonly ReferenceAttribute Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// The mod this file belongs to, if any.
    /// </summary>
    public static readonly ReferenceAttribute Mod = new(Namespace, nameof(Mod));
    
    /// <summary>
    /// The location the file will be installed to
    /// </summary>
    public static readonly GamePathAttribute To = new(Namespace, nameof(To));
    
    /// <summary>
    /// Standard model for a file.
    /// </summary>
    /// <param name="tx"></param>
    public class Model(ITransaction tx) : Entity(tx)
    {

        /// <summary>
        /// Gets the file id
        /// </summary>
        public FileId FileId => FileId.From(Id);

        public LoadoutId LoadoutId
        {
            get => LoadoutId.From(File.Loadout.Get(this));
            set => File.Loadout.Add(this, value.Value);
        }

        public Loadout.Model Loadout
        {
            get => Db.Get<Loadout.Model>(LoadoutId.Value);
            set => File.Loadout.Add(this, value.Id);
        }
        
        /// <summary>
        /// The location the file will be installed to
        /// </summary>
        public GamePath To
        {
            get => File.To.Get(this);
            set => File.To.Add(this, value);
        }
        
        public EntityId ModId
        {
            get => File.Mod.Get(this);
            set => File.Mod.Add(this, value);
        }
        
        public Mod.Model Mod
        {
            get => Db.Get<Mod.Model>(ModId);
            set => File.Mod.Add(this, value.Id);
        }
        
        /// <summary>
        /// If this file is a stored file, this will return true and cast the stored file to the out parameter.
        /// </summary>
        public bool TryGetAsStoredFile([NotNullWhen(true)] out StoredFile.Model? storedFile)
        {
            storedFile = null;
            if (!Contains(StoredFile.Hash)) 
                return false;
            
            storedFile = this.Remap<StoredFile.Model>();
            return true;
        }
    }
}
