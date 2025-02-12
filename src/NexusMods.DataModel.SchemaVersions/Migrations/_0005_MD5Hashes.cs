using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Adds MD5 hashes on all LibraryFile entities. Previously, MD5 hashes were only on locally added files or
/// on external collection downloads that the app downloaded directly.
/// </summary>
internal class _0005_MD5Hashes : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0005_MD5Hashes));

    private readonly IFileStore _fileStore;
    public _0005_MD5Hashes(IServiceProvider serviceProvider)
    {
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    private (LibraryFile.ReadOnly entity, Md5HashValue)[] _entitiesToUpdate = [];
    private HashSet<AttributeId> _attributesToRemove = [];

    public async Task Prepare(IDb db)
    {
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

        _entitiesToUpdate = await LibraryFile
            .All(db)
            .SelectAsync(async entity =>
            {
                var hasFile = await _fileStore.HaveFile(entity.Hash);
                if (!hasFile)
                {
                    // TODO: not sure what's going on here, some library files had no associated file
                    return (entity, default(Md5HashValue));
                }

                using var algo = MD5.Create();
                await using var stream = await _fileStore.GetFileStream(entity.Hash);
                var rawHash = await algo.ComputeHashAsync(stream);
                return (entity, Md5HashValue.From(rawHash));
            })
            .ToArrayAsync();
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var tuple in _entitiesToUpdate)
        {
            var (entity, md5) = tuple;

            // NOTE(erri120): need to use the IndexSegment because we don't want resolved datoms for removed attributes
            foreach (var datom in entity.IndexSegment)
            {
                if (!_attributesToRemove.TryGetValue(datom.A, out var attributeToRemove)) continue;
                tx.Add(datom.Retract());
            }

            if (md5 == default(Md5HashValue)) continue;
            tx.Add(entity.Id, LibraryFile.Md5, md5);
        }
    }
}
