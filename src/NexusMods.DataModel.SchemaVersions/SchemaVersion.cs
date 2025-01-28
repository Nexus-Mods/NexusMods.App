using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.SchemaVersions;

public partial class SchemaVersion : IModelDefinition
{
    public const string Namespace = "NexusMods.DataModel.SchemaVersioning.SchemaVersionModel";

    /// <summary>
    /// The current fingerprint of the database. This is used to detect when schema updates do not need to be performend,
    /// and the app can start without the rather expensive upgrade process.
    /// </summary>
    public static readonly MigrationIdAttribute CurrentVersion = new(Namespace, "CurrentVersion");
}
