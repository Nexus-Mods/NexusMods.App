using System.Security.Cryptography;
using DynamicData.Kernel;
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

    private Datom[] _datoms = [];

    public Task Prepare(IDb db)
    {
        var attributesToRemove = db.AttributeCache.AllAttributeIds
            .Where(sym =>
            {
                // NOTE(erri120): These attributes have been removed
                if (sym.Namespace == "NexusMods.Library.LocalFile" && sym.Name == "Md5") return true;
                if (sym.Namespace == "NexusMods.Collections.DirectDownloadLibraryFile" && sym.Name == "Md5") return true;
                return false;
            })
            .Select(sym => db.AttributeCache.GetAttributeId(sym))
            .ToHashSet();

        _datoms = LibraryFile
            .All(db)
            .Select(entity =>
            {
                // NOTE(erri120): need to use the IndexSegment because we don't want resolved datoms for removed attributes
                foreach (var datom in entity.IndexSegment)
                {
                    if (attributesToRemove.Contains(datom.A)) return datom;
                }

                return Optional<Datom>.None;
            })
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value)
            .ToArray();

        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var datom in _datoms)
        {
            tx.Add(datom.Retract());
            var md5 = Md5HashValue.From(UInt128Serializer.Read(datom.ValueSpan));
            if (md5 != default(Md5HashValue))
            {
                tx.Add(datom.E, LibraryFile.Md5, md5);
            }
        }
    }
}
