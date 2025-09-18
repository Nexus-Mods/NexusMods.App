using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Jobs;
using NexusMods.Sdk.Hashes;
using NexusMods.Hashing.xxHash3;
using NexusMods.Networking.GOG.DTOs;
using NexusMods.Networking.GOG.Exceptions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Sdk;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Networking.GOG.CLI;

public static class Verbs
{
    internal static IServiceCollection AddGOGVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("gog", "Verbs for interacting with GOG")
            .AddModule("gog app", "Verbs for querying GOG apps")
            .AddVerb(() => Login)
            .AddVerb(() => Index);

    [Verb("gog login", "Logs into GOG")]
    private static async Task<int> Login([Injected] IClient client)
    {
        await client.Login(CancellationToken.None);
        return 0;
    }
    
    [Verb("gog app index", "Indexes a GOG app and updates the given output folder")]
    private static async Task<int> Index(
        [Injected] IRenderer renderer,
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] IClient client, 
        [Option("p", "productId", "Product Id to index")] long productId,
        [Option("o", "output", "The output folder to write the index to")] AbsolutePath output,
        [Option("v", "verify", "Verify the files are hashed properly. This takes longer to execute but ensures the files are downloaded correctly.", true)] bool verify,
        [Injected] CancellationToken token)
    {

        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        await using (var _ = await renderer.WithProgress())
        {
            {
                var hashPathRoot = output / "hashes";
                hashPathRoot.CreateDirectory();
                var existingHashes = await LoadExistingHashes(hashPathRoot, indentedOptions, token);


                // TODO: await HandleLinuxInstallers(client, ProductId.From((ulong)productId), token);

                await foreach (var os in Enum.GetValues<OS>().WithProgress(renderer, "Operating Systems Builds").WithCancellation(token))
                {
                    var builds = await client.GetBuilds(ProductId.From((ulong)productId), os, token);

                    await foreach (var build in builds.WithProgress(renderer, $"{os} Builds").WithCancellation(token))
                    {
                        var buildPath = output / "stores" / "gog" / "builds" / (build.BuildId + ".json");
                        buildPath.Parent.CreateDirectory();
                        {
                            buildPath.Parent.CreateDirectory();
                            await using var outputStream = buildPath.Create();
                            await JsonSerializer.SerializeAsync(outputStream, build, indentedOptions,
                                token
                            );
                        }

                        var buildDetails = await client.GetBuildDetails(build, token);
                        var buildDetailsPath = output / "stores" / "gog" / "build_details" / (build.BuildId + ".json");

                        var depotItems = buildDetails.Depots;
                        Dictionary<string, AbsolutePath> manifestPaths = new();
                        Dictionary<string, AbsolutePath> foundHashesPaths = new();

                        foreach (var depot in depotItems)
                        {
                            var manifestPath = output / "stores" / "gog" / "manifest" / (depot.Manifest + ".json");
                            manifestPaths[depot.Manifest] = manifestPath;
                            
                            var foundHashesPath = output / "stores" / "gog" / "found_hashes" / (depot.Manifest + ".json");
                            foundHashesPaths[depot.Manifest] = foundHashesPath;
                        }

                        if (manifestPaths.Values.All(p => p.FileExists) && foundHashesPaths.Values.All(p => p.FileExists) && buildDetailsPath.FileExists)
                        {
                            await renderer.TextLine($"Build {build.BuildId} already indexed, skipping");
                            continue;
                        }

                        // Create the directory
                        buildDetailsPath.Parent.CreateDirectory();
                        manifestPaths.Values.First().Parent.CreateDirectory();
                        foundHashesPaths.Values.First().Parent.CreateDirectory();
                        
                        // Write the build manifest
                        {
                            await using var outputStream = buildDetailsPath.Create();
                            await JsonSerializer.SerializeAsync(outputStream, buildDetails, indentedOptions,
                                token
                            );
                        }

                        List<(string ManifestId, BuildDetailsDepot BuildDepot, DepotInfo Manifest)> depotInfos = [];

                        await Parallel.ForEachAsync(depotItems, token, async (depot, token) =>
                            {
                                var manifest = await client.GetDepot(depot, token);
                                lock (depotInfos)
                                {
                                    depotInfos.Add((depot.Manifest, depot, manifest));
                                }
                            }
                        );
                        
                        await renderer.TextLine($"Found {depotInfos.Count} depots to index");

                        foreach (var depot in depotInfos)
                        {
                            await using var manifestFile = manifestPaths[depot.ManifestId].Create();
                            await JsonSerializer.SerializeAsync(manifestFile, depot.Manifest, indentedOptions, token);
                        }

                        var hashLock = new SemaphoreSlim(1, 1);

                        
                        await renderer.TextLine("Indexing Existing Hashes.");


                        await renderer.TextLine("Indexing Files");
                        // Now we need to hash all the files, and write the hash variants as well as the foundHashes mapping
                        try
                        {
                            await Parallel.ForEachAsync(depotInfos, token, async (row, _) =>
                                {
                                    var (manifestId, depot, manifest) = row;
                                    ConcurrentDictionary<string, Hash> foundHashes = new();

                                    await Parallel.ForEachAsync(manifest.Items, token, async (item, _) =>
                                        {
                                            // If we have a MD5 hash and we've already indexed it, don't re-index it
                                            if (item.Md5.HasValue && existingHashes.TryGetValue(item.Md5.Value, out var hash))
                                            {
                                                foundHashes.TryAdd(item.Path, hash.XxHash3);
                                                return;
                                            }

                                            var fullSize = item.Chunks.Sum(s => (double)s.Size.Value);
                                            {
                                                await using var progressTask = await renderer.StartProgressTask($"Hashing {item.Path}", maxValue: fullSize);
                                                await using var stream = await client.GetFileStream(depot.ProductId, manifest, item.Path,
                                                    token
                                                );
                                                await using var progressWrapper = new StreamProgressWrapper<ProgressTask>(stream, state: progressTask, notifyWritten: static (progressTask, values) =>
                                                    {
                                                        var (current, _) = values;
                                                        var task = progressTask.Increment(current.Value);
                                                    }
                                                );

                                                try
                                                {
                                                    var multiHash = await MultiHasher.HashStream(stream, cancellationToken: token);
                                                    var hashStr = multiHash.XxHash3.ToString()[2..];

                                                    foundHashes.TryAdd(item.Path, multiHash.XxHash3);

                                                    var path = hashPathRoot / $"{hashStr[..2]}" / (hashStr + ".json");
                                                    path.Parent.CreateDirectory();

                                                    if (!path.FileExists)
                                                    {
                                                        try
                                                        {
                                                            await hashLock.WaitAsync(token);
                                                            await using var outputStream = path.Create();
                                                            await JsonSerializer.SerializeAsync(outputStream, multiHash, indentedOptions,
                                                                token
                                                            );
                                                        }
                                                        finally
                                                        {
                                                            hashLock.Release();
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"Error hashing {item.Path}: {ex.Message}");
                                                }
                                            }
                                        }
                                    );

                                    try
                                    {
                                        await hashLock.WaitAsync(token);
                                        // Now write the found hashes mapping
                                        var foundHashesPath = foundHashesPaths[manifestId];
                                        await using var foundHashStream = foundHashesPath.Create();
                                        await JsonSerializer.SerializeAsync(foundHashStream, foundHashes, indentedOptions,
                                            token
                                        );
                                    }
                                    finally
                                    {
                                        hashLock.Release();
                                    }
                                }
                            );
                        }
                        catch (ForbiddenException)
                        {
                            await renderer.TextLine($"Skipping {build.BuildId} because it is forbidden");
                            continue;
                        }
                    }
                }
            }
        }
        
        await renderer.Text("Indexing complete");

        return 0;
    }

    private static async Task HandleLinuxInstallers(
        IClient client,
        ProductId productId,
        CancellationToken cancellationToken)
    {
        var installers = await client.GetInstallers(ProductId.From((ulong)productId), cancellationToken: cancellationToken);
        if (!installers.TryGetFirst(x => x.OS == OSPlatform.Linux, out var linuxInstaller)) return;

        // TODO: use something else for production, only using MemoryStream for testing
        using var ms = new MemoryStream();
        await client.DownloadInstallerArchive(linuxInstaller, ms, cancellationToken: cancellationToken);
        ms.Position = 0;

        // NOTE(erri120): this magic directory contains the actual game data
        var targetDirectory = RelativePath.FromUnsanitizedInput("data/noarch");

        // NOTE(erri120): gameinfo file has the product and build IDs
        var gameInfoFile = targetDirectory / "gameinfo";

        using var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read);
        var gameInfoEntry = zipArchive.GetEntry(gameInfoFile.Path);
        var gameInfo = gameInfoEntry is null ? null : ParseGameInfoFile(gameInfoEntry);

        foreach (var zipEntry in zipArchive.Entries)
        {
            var originalPath = zipEntry.FullName;
            var relativePath = RelativePath.FromUnsanitizedInput(originalPath);

            if (!relativePath.StartsWith(targetDirectory)) continue;
            relativePath = relativePath.DropFirst(numDirectories: targetDirectory.Depth + 1);

            // TODO: put in DB
        }
    }

    private static async Task<Dictionary<Md5Value, MultiHash>> LoadExistingHashes(AbsolutePath folder, JsonSerializerOptions options, CancellationToken token)
    {
        var bag = new Dictionary<Md5Value, MultiHash>();
        var hashFiles = folder.EnumerateFiles("*.json", true);
        
        await Parallel.ForEachAsync(hashFiles, token, async (file, token) =>
        {
            try
            {
                await using var stream = file.Read();
                var hash = await JsonSerializer.DeserializeAsync<MultiHash>(stream, options, token);
                lock (bag)
                {
                    bag.Add(hash!.Md5, hash);
                }
            }
            catch (Exception ex)
            {
                // Ignore errors
                Console.WriteLine($"Error loading hash file {file}: {ex.Message}");
            }
        });

        return bag;
    }

    
    private static GameInfo ParseGameInfoFile(ZipArchiveEntry zipArchiveEntry)
    {
        using var stream = zipArchiveEntry.Open();
        var sr = new StreamReader(stream);
        var contents = sr.ReadToEnd();
        return ParseGameInfoFile(contents);
    }

    private static GameInfo ParseGameInfoFile(ReadOnlySpan<char> contents)
    {
        var lineEnumerator = contents.EnumerateLines();
        var lineCount = -1;

        string? gameName = null;
        string? vanityVersion = null;
        string? language = null;
        ProductId productId = default;
        BuildId buildId = default;

        foreach (var line in lineEnumerator)
        {
            lineCount++;

            if (lineCount == 0)
            {
                gameName = line.ToString();
            } else if (lineCount == 1)
            {
                vanityVersion = line.ToString();
            } else if (lineCount == 2)
            {
                // unknown
            } else if (lineCount == 3)
            {
                language = line.ToString();
            } else if (lineCount == 4)
            {
                productId = ProductId.From(ulong.Parse(line));
            } else if (lineCount == 5)
            {
                // appears to be a duplicate entry for gameId
            } else if (lineCount == 6)
            {
                buildId = BuildId.From(ulong.Parse(line));
            }
        }

        return new GameInfo(gameName, vanityVersion, language, productId, buildId);
    }

    private record GameInfo(string? GameName, string? VanityVersion, string? Language, ProductId ProductId, BuildId BuildId);
}
