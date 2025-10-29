using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

public interface TransactionalMigration : IMigration
{
    /// <summary>
    /// Run the migration inserting changes into the given transaction
    /// </summary>
    public void Migrate(Transaction tx, IDb db);
}
