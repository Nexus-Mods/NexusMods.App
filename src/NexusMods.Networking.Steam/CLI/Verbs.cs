using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Networking.Steam.Exceptions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.ProxyConsole;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.Paths.Utilities;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;

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
            .AddVerb(() => PackManifestsFromGame)
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

        var resolved = TryResolveManifest([(steamAppId, productInfo)], (ulong)manifestId, branch);
        if (resolved is null)
        {
            await renderer.Text($"Manifest {manifestId} not found for app {appId}{(string.IsNullOrWhiteSpace(branch) ? "" : $" on branch '{branch}'")}.");
            return 1;
        }

        var (app, depot, usedBranch, manifest) = resolved.Value;

        // Ensure .nx extension
        var finalOutput = output;
        if (!finalOutput.ToString().EndsWith(KnownExtensions.Nx.ToString(), StringComparison.OrdinalIgnoreCase))
            finalOutput = finalOutput.AppendExtension(KnownExtensions.Nx);

        await renderer.TextLine($"Preparing to pack manifest {manifest.Value} (depot {depot.Value}, branch {usedBranch})…");
        await PackManifestToNx(steamSession, renderer, app, depot, manifest, usedBranch, finalOutput, token);
        await renderer.TextLine($"Done: {finalOutput}");
        return 0;
    }

    [Verb("steam manifest pack-game", "Packs all Steam manifests for an installed game into separate .nx archives")]
    private static async Task<int> PackManifestsFromGame(
        [Injected] IRenderer renderer,
        [Injected] ISteamSession steamSession,
        [Injected] IGameRegistry gameRegistry,
        [Option("g", "game", "Installed game name")] string gameName,
        [Option("o", "output", "Output directory for .nx files")] AbsolutePath output,
        [Injected] CancellationToken token)
    {
        // Find installed game by name (Steam only)
        var installs = gameRegistry.InstalledGames
            .Where(g => string.Equals(g.Game.Name, gameName, StringComparison.OrdinalIgnoreCase))
            .Where(g => g.Store == GameStore.Steam)
            .ToList();

        if (installs.Count == 0)
        {
            await renderer.TextLine($"No installed Steam game found with name '{gameName}'.");
            return 1;
        }

        if (installs.Count > 1)
        {
            await renderer.TextLine($"Multiple installed Steam games matched '{gameName}', using the first match: {installs[0].Game.Name}.");
        }

        var install = installs[0];
        if (install.LocatorResultMetadata is not SteamLocatorResultMetadata steamMeta)
        {
            await renderer.TextLine("The matched installation does not have Steam metadata; cannot determine manifests.");
            return 1;
        }

        var baseAppId = AppId.From(steamMeta.AppId);

        RenderingAuthenticationHandler.Renderer = renderer;
        await steamSession.Connect(token);

        // Ensure output directory exists
        output.CreateDirectory();

        // Build the set of appIds to search depots in: base app + any declared SteamIds on the game
        var appIdsToQuery = new HashSet<uint> { baseAppId.Value };
        if (install.Game is ISteamGame steamGame)
        {
            foreach (var id in steamGame.SteamIds)
                appIdsToQuery.Add(id);
        }

        // Fetch product info for all candidate apps
        var productInfos = new List<(AppId AppId, ProductInfo Info)>();
        foreach (var rawId in appIdsToQuery)
        {
            var appId = AppId.From(rawId);
            try
            {
                await renderer.TextLine($"Fetching product info for app {appId.Value}…");
                var info = await steamSession.GetProductInfoAsync(appId, token);
                productInfos.Add((appId, info));
            }
            catch (Exception ex)
            {
                await renderer.TextLine($"Failed to get product info for app {appId.Value}: {ex.Message}");
            }
        }

        // Resolver using shared helper across all candidate apps
        (AppId appId, DepotId depotId, string branch, ManifestId manifestId)? ResolveCandidate(ulong manifestIdValue)
            => TryResolveManifest(productInfos.Select(pi => (pi.AppId, pi.Info)), manifestIdValue);

        var manifestIds = steamMeta.ManifestIds.Distinct().ToArray();
        if (manifestIds.Length == 0)
        {
            await renderer.TextLine("No manifest IDs found for this installation.");
            return 1;
        }

        await renderer.TextLine($"Preparing to pack {manifestIds.Length} manifest(s) for '{install.Game.Name}'.");

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4),
            CancellationToken = token,
        };

        var results = new ConcurrentDictionary<ulong, string>();

        await Parallel.ForEachAsync(manifestIds, options, async (manifestIdValue, ct) =>
        {
            try
            {
                var candidate = ResolveCandidate(manifestIdValue);
                if (candidate is null)
                {
                    await renderer.TextLine($"- Manifest {manifestIdValue} not found in product info; skipping.");
                    results[manifestIdValue] = "notfound";
                    return;
                }

                var (appId, depotId, branch, manifestId) = candidate.Value;

                // Output path: <output>/<manifestId>.nx
                var outputPath = output / (manifestId.Value + KnownExtensions.Nx.ToString()).ToRelativePath();
                if (outputPath.FileExists)
                {
                    await renderer.TextLine($"- Skipping {manifestId.Value}: output exists {outputPath}");
                    results[manifestIdValue] = "skipped_exists";
                    return;
                }

                await renderer.TextLine($"- Packing manifest {manifestId.Value} (depot {depotId.Value}, branch {branch})…");
                await PackManifestToNx(steamSession, renderer, appId, depotId, manifestId, branch, outputPath, ct);
                await renderer.TextLine($"- Done: {outputPath}");
                results[manifestIdValue] = "packed";
            }
            catch (FailedToGetRequestCode ex)
            {
                await renderer.TextLine($"- Skipping manifest {manifestIdValue}: {ex.Message}");
                results[manifestIdValue] = "auth_error";
            }
            catch (Exception ex)
            {
                await renderer.TextLine($"- Error packing manifest {manifestIdValue}: {ex.Message}");
                results[manifestIdValue] = "error";
            }
        });

        // Post-check: confirm all reported manifests were processed
        var processed = results.Keys.ToHashSet();
        var missing = manifestIds.Where(id => !processed.Contains(id)).ToArray();
        var notFound = results.Where(kv => kv.Value == "notfound").Select(kv => kv.Key).OrderBy(x => x).ToArray();
        var errored = results.Where(kv => kv.Value is "error" or "auth_error").Select(kv => kv.Key).OrderBy(x => x).ToArray();

        var packedCount = results.Count(kv => kv.Value == "packed");
        var skippedCount = results.Count(kv => kv.Value == "skipped_exists");
        await renderer.TextLine($"Finished packing manifests. Packed: {packedCount}, Skipped existing: {skippedCount}, Not found: {notFound.Length}, Errors: {errored.Length}.");
        if (missing.Length > 0 || notFound.Length > 0 || errored.Length > 0)
        {
            if (notFound.Length > 0)
                await renderer.TextLine($"Not found in accessible depots: {string.Join(", ", notFound)}");
            if (errored.Length > 0)
                await renderer.TextLine($"Failed due to errors: {string.Join(", ", errored)}");

            var searchedAppIds = string.Join(", ", productInfos.Select(pi => pi.AppId.Value).Distinct());
            await renderer.TextLine($"Searched appIds: {searchedAppIds}. Some manifests may belong to DLC/appIds not included here.");
        }
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

        await using var _ = await renderer.WithProgress();
        
        var productInfo = await steamSession.GetProductInfoAsync(steamAppId, token);

        var hashFolder = output / "hashes";
        hashFolder.CreateDirectory();

        var existingHashes = await LoadExistingHashes(hashFolder, indentedOptions, token);

        // Write the product info to a file
        var productFile = output / "stores" / "steam" / "apps" / (productInfo.AppId + ".json");
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

        return 0;
    }

    /// <summary>
    /// Helper: resolve a manifest to (appId, depotId, branch, manifestId) across one or more product infos
    /// </summary>
    private static (AppId appId, DepotId depotId, string branch, ManifestId manifestId)? TryResolveManifest(IEnumerable<(AppId appId, ProductInfo info)> productInfos, ulong manifestId, string? branchFilter = null)
    {
        var candidates = productInfos
            .SelectMany(pi => pi.info.Depots
                .SelectMany(d => d.Manifests.Select(kv => new { pi.appId, Depot = d.DepotId, Branch = kv.Key, Info = kv.Value })))
            .Where(x => x.Info.ManifestId.Value == manifestId)
            .ToList();

        if (!string.IsNullOrWhiteSpace(branchFilter))
            candidates = candidates.Where(c => string.Equals(c.Branch, branchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return (candidates[0].appId, candidates[0].Depot, candidates[0].Branch, candidates[0].Info.ManifestId);

        // Prefer 'public' branch when ambiguous
        var chosen = candidates.FirstOrDefault(c => string.Equals(c.Branch, "public", StringComparison.OrdinalIgnoreCase)) ?? candidates[0];
        return (chosen.appId, chosen.Depot, chosen.Branch, chosen.Info.ManifestId);
    }

    /// <summary>
    /// Packs the specified Steam manifest into the .nx format and writes it to the specified output location.
    /// </summary>
    /// <param name="steamSession">The Steam session instance used to retrieve the manifest contents.</param>
    /// <param name="renderer">An instance of the renderer for outputting text feedback during the process.</param>
    /// <param name="appId">The ID of the application for the manifest being packed.</param>
    /// <param name="depotId">The ID of the depot associated with the manifest.</param>
    /// <param name="manifestId">The unique identifier for the manifest to be packed.</param>
    /// <param name="branch">The branch associated with the manifest.</param>
    /// <param name="finalOutput">The path where the generated .nx archive will be saved.</param>
    /// <param name="token">A cancellation token to observe while performing the operation.</param>
    /// <returns>A task that represents the asynchronous operation of packing the manifest.</returns>
    private static async Task PackManifestToNx(
        ISteamSession steamSession, IRenderer renderer, AppId appId, DepotId depotId, ManifestId manifestId, string branch, AbsolutePath finalOutput, CancellationToken token)
    {
        var manifest = await steamSession.GetManifestContents(appId, depotId, manifestId,
            branch, token
        );

        var builder = new NxPackerBuilder();
        builder.WithMaxNumThreads(Environment.ProcessorCount);
        var openStreams = new List<Stream>(manifest.Files.Length);

        foreach (var file in manifest.Files)
        {
            var stream = file.Size == Size.Zero
                ? Stream.Null
                : steamSession.GetFileStream(appId, manifest, file.Path);
            if (!ReferenceEquals(stream, Stream.Null))
                openStreams.Add(stream);
            builder.AddFile(stream, new AddFileParams
            {
                RelativePath = file.Path.ToString(),
            });
        }

        var tmpOutput = finalOutput.ReplaceExtension(KnownExtensions.Tmp);
        await renderer.TextLine("Writing .nx archive…");
        await using (var outputStream = tmpOutput.Create())
        {
            builder.WithOutput(outputStream);
            builder.Build();
        }

        foreach (var s in openStreams)
            await s.DisposeAsync();

        await tmpOutput.MoveToAsync(finalOutput, token: token);
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
