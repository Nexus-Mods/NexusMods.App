using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Turns all DirectDownloadLibraryFile entities into LocalFile entities by adding the LocalFile.OriginalPath attribute.
/// Also updates the LibraryItem.Name attribute to use the provided LogicalFileName instead of the temporary file name.
/// </summary>
internal class _0006_DirectDownload : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0006_DirectDownload));

    private DirectDownloadLibraryFile.ReadOnly[] _entitiesToUpdate = [];

    public Task Prepare(IDb db)
    {
        _entitiesToUpdate = DirectDownloadLibraryFile.All(db).Where(entity => !LocalFile.OriginalPath.IsIn(entity)).ToArray();
        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var entity in _entitiesToUpdate)
        {
            tx.Add(entity.Id, LocalFile.OriginalPath, entity.LogicalFileName);
            tx.Add(entity.Id, LibraryItem.Name, entity.LogicalFileName);
        }
    }
}
