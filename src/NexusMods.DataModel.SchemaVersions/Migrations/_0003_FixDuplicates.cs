using System.Runtime.InteropServices;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Migration to fix issue https://github.com/Nexus-Mods/NexusMods.App/issues/2608.
/// See PR https://github.com/Nexus-Mods/NexusMods.App/pull/2610 issue analysis.
///
/// This PR removes duplicate file and mod page metadata entities by updating every reference
/// to point to the same entity.
/// </summary>
internal class _0003_FixDuplicates : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0003_FixDuplicates));

    private Dictionary<EntityId, EntityId> _mappings = [];
    public async Task Prepare(IDb db)
    {
        await Task.Yield();
        var duplicateFiles = NexusModsFileMetadata
            .All(db)
            .GroupBy(static file => file.Uid)
            .Where(static grouping => grouping.Count() > 1)
            .ToDictionary(static grouping => grouping.Key, static grouping => grouping.ToArray());

        var duplicateMods = NexusModsModPageMetadata
            .All(db)
            .GroupBy(static mod => mod.Uid)
            .Where(static grouping => grouping.Count() > 1)
            .ToDictionary(static grouping => grouping.Key, static grouping => grouping.ToArray());

        foreach (var values in duplicateFiles.Values)
        {
            var main = values.First();
            foreach (var other in values.Skip(1))
            {
                _mappings[other.Id] = main.Id;
            }
        }

        foreach (var values in duplicateMods.Values)
        {
            var main = values.First();
            foreach (var other in values.Skip(1))
            {
                _mappings[other.Id] = main.Id;
            }
        }
    }

    public unsafe void Migrate(ITransaction tx, IDb db)
    {
        Memory<byte> memory = new Memory<byte>(new byte[sizeof(EntityId)]);

        foreach (var kv in _mappings)
        {
            var (oldId, newId) = kv;
            var datoms = db.ReferencesTo(oldId);

            foreach (var datom in datoms)
            {
                MemoryMarshal.Write(memory.Span, newId);
                tx.Add(new Datom(datom.Prefix, memory));
            }

            tx.Delete(oldId, recursive: false);
        }
    }
}
