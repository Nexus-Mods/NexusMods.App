using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

public interface ITransactionalMigration : IMigration
{
    /// <summary>
    /// Run the migration inserting changes into the given transaction
    /// </summary>
    public void Migrate(ITransaction tx, IDb db);
}
