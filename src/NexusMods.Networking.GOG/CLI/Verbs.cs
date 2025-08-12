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
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Sdk.Hashes;
using NexusMods.Hashing.xxHash3;
using NexusMods.Networking.GOG.DTOs;
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

                        var depot = await client.GetDepot(build, token);

                        var depotPath = output / "stores" / "gog" / "depots" / (build.BuildId + ".json");
                        var foundHashesPath = output / "stores" / "gog" / "found_hashes" / (build.BuildId + ".json");

                        if (depotPath.FileExists && foundHashesPath.FileExists)
                            continue;

                        depotPath.Parent.CreateDirectory();
                        {
                            depotPath.Parent.CreateDirectory();
                            await using var outputStream = depotPath.Create();
                            await JsonSerializer.SerializeAsync(outputStream, depot, indentedOptions,
                                token
                            );
                        }

                        var hashPathRoot = output / "hashes";
                        hashPathRoot.CreateDirectory();

                        var hashLock = new SemaphoreSlim(1, 1);

                        ConcurrentDictionary<string, Hash> foundHashes = new();

                        await Parallel.ForEachAsync(depot.Items, token, async (item, token) =>
                            {
                                var fullSize = item.Chunks.Sum(s => (double)s.Size.Value);
                                {
                                    await using var task = await renderer.StartProgressTask($"Hashing {item.Path}", maxValue: fullSize);
                                    await using var stream = await client.GetFileStream(build, depot, item.Path,
                                        token
                                    );
                                    var multiHasher = new MultiHasher();
                                    var multiHash = await multiHasher.HashStream(stream, token, async size => await task.Increment(size.Value));

                                    var hashStr = multiHash.XxHash3.ToString()[2..];

                                    foundHashes.TryAdd(item.Path, multiHash.XxHash3);

                                    var path = hashPathRoot / $"{hashStr[..2]}" / (hashStr.ToRelativePath() + ".json");
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

                                if (verify)
                                {
                                    var offset = Size.Zero;
                                    await using var stream = await client.GetFileStream(build, depot, item.Path,
                                        token
                                    );
                                    await foreach (var chunk in item.Chunks.WithProgress(renderer, $"Verifying {item.Path}"))
                                    {
                                        using var rented = MemoryPool<byte>.Shared.Rent((int)chunk.Size.Value);
                                        var sized = rented.Memory[..(int)chunk.Size.Value];

                                        await stream.ReadExactlyAsync(sized, token);

                                        if ((ulong)stream.Position - offset.Value != chunk.Size.Value)
                                            throw new InvalidOperationException("Chunk size mismatch");

                                        offset += chunk.Size;


                                        var md5 = Md5Value.From(MD5.HashData(sized.Span));
                                        if (!md5.Equals(chunk.Md5))
                                            throw new InvalidOperationException("MD5 mismatch");

                                    }
                                }
                            }
                        );

                        foundHashesPath.Parent.CreateDirectory();
                        await using var foundHashStream = foundHashesPath.Create();
                        await JsonSerializer.SerializeAsync(foundHashStream, foundHashes, indentedOptions,
                            token
                        );
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
