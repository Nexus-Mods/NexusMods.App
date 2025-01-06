using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Games.GameHashes.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using RocksDbSharp;
using static NexusMods.Paths.Utilities.KnownExtensions;

namespace NexusMods.Games.GameHashes;

public static class Verbs
{
    internal static IServiceCollection AddGameHashesVerbs(this IServiceCollection collection) =>
        collection
            .AddVerb(() => Pack);
    
    [Verb("pack-game-hashes", "Packs a folder of game hashes into a zipped MnemonicDB database.")]
    private static async Task<int> Pack([Option("i", "inputFolder", "The source folder (git repo)")] AbsolutePath inputFolder,
    [Option("o", "outputfile", "The output file to create")] AbsolutePath outputFile, 
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IServiceProvider provider,
        [Injected] JsonSerializerOptions jsonOptions)
    {
        await using var backendLocation = temporaryFileManager.CreateFolder(); 
        
        var backend = new Backend();
        var settings = new DatomStoreSettings
        {
            Path = backendLocation.Path,
        };
        var datomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        var conn = new Connection(provider.GetRequiredService<ILogger<Connection>>(), datomStore, provider, []);

        var field = backend.GetType().GetField("Db", BindingFlags.NonPublic | BindingFlags.Instance);
        var rocksDb = (RocksDb)field?.GetValue(backend);
        
        try
        {
            await AddGameHashes(inputFolder, conn, jsonOptions);
            
            await AddSteamManifestEntries(inputFolder, conn, jsonOptions);
            
            await AddGOGBuildEntries(inputFolder, conn, jsonOptions);

 

            var db = conn.Db;
            Console.WriteLine($"Wrote {HashRelation.All(db).Count} hashes");
            Console.WriteLine($"Wrote {SteamManifestEntry.All(db).Count} steam manifest entries");
            Console.WriteLine($"Wrote {GOGBuildEntry.All(db).Count} GOG build entries");
            
            
            Native.Instance.rocksdb_backup_engine_o
            
            rocksDb!.Cr
            rocksDb!.Flush(new FlushOptions().SetWaitForFlush(true));
            // Shut down the connection
            datomStore.Dispose();
            backend.Dispose();
            
            
            if (outputFile.FileExists)
                outputFile.Delete();
            
            // Zip the database 
            ZipFile.CreateFromDirectory(backendLocation.Path.ToString(), outputFile.ToString(), CompressionLevel.SmallestSize,
                false
            );
        }
        catch (Exception e)
        {
            return 1;
        }

        return 0;
    }

    private static async Task AddGOGBuildEntries(AbsolutePath inputFolder, Connection conn, JsonSerializerOptions jsonOptions)
    {
        using var tx = conn.BeginTransaction();
        var db = conn.Db;

        foreach (var hashMapping in (inputFolder / "json/stores/gog/found_hashes").EnumerateFiles(Json))
        {
            if (!ulong.TryParse(hashMapping.GetFileNameWithoutExtension(), out var parsedBuildId))
                continue;
                
            await using var hashMappingStream = hashMapping.Read();
            var mappings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(hashMappingStream, jsonOptions);
            
            if (mappings is null)
                continue;

            foreach (var (path, hash) in mappings)
            {
                var tmp = hash.Chunk(2).Reverse().SelectMany(s => s);

                var parsedHash = Hash.FromHex(new string(tmp.ToArray()));//  Hash.FromHex(hash);
                var foundHash = HashRelation.FindByHash(db, parsedHash).FirstOrDefault();
                
                if (!foundHash.IsValid())
                    continue;

                _ = new GOGBuildEntry.New(tx)
                {
                    BuildId = BuildId.From(parsedBuildId),
                    Path = path,
                    HashId = foundHash,
                };
            }

        }
        await tx.Commit();
    }

    private static async Task AddSteamManifestEntries(AbsolutePath inputFolder, Connection conn, JsonSerializerOptions jsonOptions)
    {
        using var tx = conn.BeginTransaction();
        var db = conn.Db;
        foreach (var file in (inputFolder / "json/stores/steam/manifests").EnumerateFiles(Json))
        {
            await using var stream = file.Read();
            var manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, jsonOptions);
            
            if (manifest is null)
                continue;
            
            foreach (var fileEntry in manifest.Files)
            {
                // Directories don't have chunks
                if (fileEntry.Chunks.Length == 0)
                    continue;
                
                var hashEntry = HashRelation.FindBySha1(db, fileEntry.Hash).FirstOrDefault();
                
                if (!hashEntry.IsValid())
                    throw new Exception($"Hash ({fileEntry.Hash}) not found in the database");
                
                _ = new SteamManifestEntry.New(tx)
                {
                    ManifestId = manifest.ManifestId,
                    Path = fileEntry.Path,
                    HashId = hashEntry,
                };
            }
        }
        await tx.Commit();
    }

    private static async Task AddGameHashes(AbsolutePath inputFolder, Connection conn, JsonSerializerOptions jsonOptions)
    {
        using var tx = conn.BeginTransaction();
        foreach (var file in (inputFolder / "json/hashes").EnumerateFiles(Json))
        {
            await using var stream = file.Read();
            var multi = await JsonSerializer.DeserializeAsync<MultiHash>(stream, jsonOptions);
            if (multi is null)
                continue;
            
            _ = new HashRelation.New(tx)
            {
                Size = multi.Size,
                Hash = multi.XxHash3,
                MinimalistHash = multi.MinimalHash,
                Sha1 = multi.Sha1,
            };
        }
        await tx.Commit();
    }
}
