using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Jobs;
using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Networking.Steam.Exceptions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Sdk.ProxyConsole;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Networking.Steam.CLI;

public static class Verbs
{
    internal static IServiceCollection AddSteamVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("steam", "Verbs for interacting with Steam")
            .AddModule("steam app", "Verbs for querying app data")
            .AddModule("steam manifest", "Verbs for querying and packing manifest data")
            .AddVerb(() => IndexSteamApp)
            .AddVerb(() => PackManifest)
            .AddVerb(() => Login);
    
    [Verb("steam login", "Starts the login process for Steam")]
    private static async Task<int> Login(
        [Injected] IRenderer renderer,
        [Injected] ISteamSession steamSession,
        [Injected] CancellationToken token)
    {
        await steamSession.Connect(token);
        return 0;
    }

    [Verb("steam manifest pack", "Packs all files from a Steam manifest into a .nx archive")]
    private static async Task<int> PackManifest(
        [Injected] IRenderer renderer,
        [Injected] ISteamSession steamSession,
        [Option("a", "appId", "The steam app id to search in")] long appId,
        [Option("m", "manifestId", "The specific manifest id to pack")] long manifestId,
        [Option("b", "branch", "Optional branch name to disambiguate if manifest exists on multiple branches")] string branch,
        [Option("o", "output", "The output .nx file path")] AbsolutePath output,
        [Injected] CancellationToken token)
    {
        var steamAppId = AppId.From((uint)appId);

        RenderingAuthenticationHandler.Renderer = renderer;
        await steamSession.Connect(token);

        await renderer.TextLine("Fetching product info…");
        var productInfo = await steamSession.GetProductInfoAsync(steamAppId, token);
        // Find the depot + branch containing the requested manifest id
        var candidates = productInfo.Depots
            .SelectMany(d => d.Manifests.Select(kv => new { Depot = d, Branch = kv.Key, Info = kv.Value }))
            .Where(x => x.Info.ManifestId.Value == (ulong)manifestId)
            .ToList();

        if (!string.IsNullOrWhiteSpace(branch))
            candidates = candidates.Where(c => string.Equals(c.Branch, branch, StringComparison.OrdinalIgnoreCase)).ToList();

        if (candidates.Count == 0)
        {
            await renderer.Text($"Manifest {manifestId} not found for app {appId}{(string.IsNullOrWhiteSpace(branch) ? "" : $" on branch '{branch}'")}.");
            return 1;
        }
        if (candidates.Count > 1)
        {
            await renderer.TextLine($"Manifest {manifestId} appears in multiple branches/depots:");
            foreach (var c in candidates)
                await renderer.TextLine($" - Depot {c.Depot.DepotId.Value} on branch '{c.Branch}'");
            await renderer.Text("Please specify --branch to disambiguate.");
            return 1;
        }

        var chosen = candidates[0];
        var depot = chosen.Depot.DepotId;
        var usedBranch = chosen.Branch;
        var manifest = await steamSession.GetManifestContents(steamAppId, depot, chosen.Info.ManifestId, usedBranch, token);

        await renderer.TextLine($"Preparing to pack {manifest.Files.Length} files from manifest {manifestId} (depot {depot.Value}, branch {usedBranch})…");

        var builder = new NxPackerBuilder();
        var openStreams = new List<Stream>(manifest.Files.Length);

        // Queue files for packing with depot-relative paths
        foreach (var file in manifest.Files)
        {
            Stream stream = file.Size == Size.Zero
                ? Stream.Null
                : steamSession.GetFileStream(steamAppId, manifest, file.Path);
            if (!ReferenceEquals(stream, Stream.Null))
                openStreams.Add(stream);
            builder.AddFile(stream, new AddFileParams
            {
                RelativePath = file.Path.ToString(),
            });
        }

        // Determine output paths and ensure .nx extension
        var finalOutput = output;
        var nxExt = new Extension(".nx");
        if (!finalOutput.ToString().EndsWith(nxExt.ToString(), StringComparison.OrdinalIgnoreCase))
            finalOutput = finalOutput.AppendExtension(nxExt);
        var tmpOutput = finalOutput.ReplaceExtension(new Extension(".tmp"));

        await renderer.TextLine("Writing .nx archive…");
        await using (var outputStream = tmpOutput.Create())
        {
            builder.WithOutput(outputStream);
            builder.Build();
        }

        foreach (var s in openStreams)
            await s.DisposeAsync();

        await tmpOutput.MoveToAsync(finalOutput, token: token);
        await renderer.TextLine($"Done: {finalOutput}");
        return 0;
    }

    
    [Verb("steam app index", "Indexes a Steam app and updates the given output folder")]
    private static async Task<int> IndexSteamApp(
        [Injected] IRenderer renderer,
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] ISteamSession steamSession,
        [Option("a", "appId", "The steam app id to index")] long appId,
        [Option("o", "output", "The output folder to write the index to")] AbsolutePath output,
        [Injected] CancellationToken token)
    {
        var steamAppId = AppId.From((uint)appId);
        
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
        };

        RenderingAuthenticationHandler.Renderer = renderer;
        await steamSession.Connect(token);

        await using (var _ = await renderer.WithProgress())
        {
            {
                var productInfo = await steamSession.GetProductInfoAsync(steamAppId, token);

                var hashFolder = output / "hashes";
                hashFolder.CreateDirectory();

                var existingHashes = await LoadExistingHashes(hashFolder, indentedOptions, token);

                // Write the product info to a file
                var productFile = output / "stores" / "steam" / "apps" / (productInfo.AppId + ".json").ToRelativePath();
                {
                    productFile.Parent.CreateDirectory();
                    await using var outputStream = productFile.Create();
                    await JsonSerializer.SerializeAsync(outputStream, productInfo, indentedOptions,
                        token
                    );
                }

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = token,
                };
                // For each depot and each manifest, download the manifest and index the files
                await Parallel.ForEachAsync(productInfo.Depots, options, async (depot, token) =>
                {
                    await Parallel.ForEachAsync(depot.Manifests, options, async (manifestInfo, token) =>
                    {
                        try
                        {
                            var manifest = await steamSession.GetManifestContents(steamAppId, depot.DepotId, manifestInfo.Value.ManifestId,
                                manifestInfo.Key, token
                            );

                            var manifestPath = output / "stores" / "steam" / "manifests" / (manifest.ManifestId + ".json").ToRelativePath();
                            {
                                manifestPath.Parent.CreateDirectory();
                                while (true)
                                {
                                    try
                                    {
                                        await using var outputStream = manifestPath.Create();
                                        await JsonSerializer.SerializeAsync(outputStream, manifest, indentedOptions,
                                            token
                                        );
                                        break;
                                    }
                                    catch (IOException)
                                    {
                                        await Task.Delay(1000, token);
                                    }
                                }
                            }

                            await IndexManifest(steamSession, renderer, steamAppId,
                                output, manifest, indentedOptions,
                                existingHashes, options
                            );
                        }
                        catch (FailedToGetRequestCode ex)
                        {
                            await renderer.Text($"Skipping because of: {ex.Message}");
                            return;
                        }
                    });
                });
            }
        }

        return 0;
    }

    private static async Task<ConcurrentBag<Sha1Value>> LoadExistingHashes(AbsolutePath folder, JsonSerializerOptions options, CancellationToken token)
    {
        var bag = new ConcurrentBag<Sha1Value>();
        var hashFiles = folder.EnumerateFiles("*.json", true);
        
        await Parallel.ForEachAsync(hashFiles, token, async (file, token) =>
        {
            try
            {
                await using var stream = file.Read();
                var hash = await JsonSerializer.DeserializeAsync<MultiHash>(stream, options, token);
                bag.Add(hash!.Sha1);
            }
            catch (Exception ex)
            {
                // Ignore errors
                Console.WriteLine($"Error loading hash file {file}: {ex.Message}");
            }
        });

        return bag;
    }

    private static async Task IndexManifest(ISteamSession session, IRenderer renderer, AppId appId, AbsolutePath output, Manifest manifest, JsonSerializerOptions indentedOptions, ConcurrentBag<Sha1Value> existingHashes, ParallelOptions options)
    {
        var writeLock = new SemaphoreSlim(1, 1);
        await Parallel.ForEachAsync(manifest.Files, options, async (file, token) =>
            {
                if (file.Size == Size.Zero)
                    return;
                if (existingHashes.Contains(file.Hash))
                    return;
                
                await using var progressTask = await renderer.StartProgressTask($"Hashing {file.Path}", maxValue: file.Size.Value);
                await using var stream = session.GetFileStream(appId, manifest, file.Path);
                await using var progressWrapper = new StreamProgressWrapper<ProgressTask>(stream, state: progressTask, notifyWritten: static (progressTask, values) =>
                {
                    var (current, _) = values;
                    var task = progressTask.Increment(current.Value);
                });

                var multiHash = await MultiHasher.HashStream(stream, cancellationToken: token);

                var fileName = multiHash.XxHash3 + ".json";
                var path = output / "hashes" / fileName[2..4] / fileName[2..];

                await writeLock.WaitAsync(token);
                if (!multiHash.Sha1.Equals(file.Hash))
                    throw new InvalidOperationException("Hash mismatch on downloaded file, expected: " + file.Hash + " got: " + multiHash.Sha1);
                
                try
                {
                    path.Parent.CreateDirectory();
                    {
                        await using var outputStream = path.Create();
                        await JsonSerializer.SerializeAsync(outputStream, multiHash, indentedOptions,
                            token
                        );
                    }
                    existingHashes.Add(multiHash.Sha1);
                }
                finally
                {
                    writeLock.Release();
                }
            }
        );
    }
}
