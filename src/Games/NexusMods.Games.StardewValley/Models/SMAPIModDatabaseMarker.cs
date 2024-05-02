using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using File = NexusMods.Abstractions.Loadouts.Files.File;
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;


public static class SMAPIModDatabaseMarker
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIModDatabaseMarker";
    
    
    public static readonly BooleanAttribute SMAPIModDatabase = new(Namespace, "SMAPIModDatabase");
    
    /// <summary>
    /// Returns true if the file contains the SMAPI mod database marker.
    /// </summary>
    public static bool IsSMAPIModDatabase(this File.Model modDatabase) => modDatabase.Contains(SMAPIModDatabase);
    
    /// <summary>
    /// Returns all the files with the SMAPI mod database marker.
    /// </summary>
    public static IEnumerable<File.Model> SMAPIModDatabases(this Loadout.Model loadout) 
        => loadout.Files.Where(modDatabase => modDatabase.IsSMAPIModDatabase());
}
