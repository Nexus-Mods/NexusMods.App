using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Moves existing MD5 hashes to LibraryFile.Md5.
/// </summary>
internal class _0005_MD5Hashes : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0005_MD5Hashes));

    private LibraryFile.ReadOnly[] _entitiesToUpdate = [];
    private HashSet<AttributeId> _attributesToRemove = [];

    public Task Prepare(IDb db)
    {
        var hasAttribute = db.AttributeCache.TryGetAttributeId(LibraryFile.Md5.Id, out var attributeId);

        _attributesToRemove = db.AttributeCache.AllAttributeIds
            .Where(sym =>
            {
                // NOTE(erri120): These attributes have been removed
                if (sym.Namespace == "NexusMods.Library.LocalFile" && sym.Name == "Md5") return true;
                if (sym.Namespace == "NexusMods.Collections.DirectDownloadLibraryFile" && sym.Name == "Md5") return true;
                return false;
            })
            .Select(sym => db.AttributeCache.GetAttributeId(sym))
            .ToHashSet();

        _entitiesToUpdate = LibraryFile
            .All(db)
            .Where(entity =>
            {
                foreach (var datom in entity.IndexSegment)
                {
                    if (_attributesToRemove.Contains(datom.A)) return true;
                }

                return false;
            })
            .ToArray();

        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var entity in _entitiesToUpdate)
        {
            Md5HashValue md5 = default(Md5HashValue);

            // NOTE(erri120): need to use the IndexSegment because we don't want resolved datoms for removed attributes
            foreach (var datom in entity.IndexSegment)
            {
                if (!_attributesToRemove.TryGetValue(datom.A, out var attributeToRemove)) continue;
                tx.Add(datom.Retract());

                md5 = Md5HashValue.From(UInt128Serializer.Read(datom.ValueSpan));
            }

            if (md5 != default(Md5HashValue))
            {
                tx.Add(entity.Id, LibraryFile.Md5, md5);
            }
        }
    }
}
