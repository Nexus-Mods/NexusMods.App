using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.Steam.CLI;

public static class Verbs
{
    internal static IServiceCollection AddSteamVerbs(this IServiceCollection collection) =>
        collection
            .AddVerb(() => IndexSteamApp);
    
    [Verb("index-steam-app", "Indexes a Steam app and updates the given output folder")]
    private static async Task<int> IndexSteamApp(
        [Injected] IRenderer renderer,
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] ISteamSession steamSession,
        [Option("a", "appId", "The Steam app ID to index")] AppId appId,
        [Option("o", "output", "The output folder to write the index to")] AbsolutePath output,
        [Injected] CancellationToken token)
    {
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
        };
        var productInfo = await steamSession.GetProductInfoAsync(appId, token);
        
        var hashFolder = output / "hashes";
        hashFolder.CreateDirectory();
        
        var existingHashes = await LoadExistingHashes(hashFolder, indentedOptions, token);
        
        // Write the product info to a file
        var productFile = output / "stores" /  "steam" / "apps" / (productInfo.AppId + ".json").ToRelativePath();
        {
            productFile.Parent.CreateDirectory();
            await using var outputStream = productFile.Create();
            await JsonSerializer.SerializeAsync(outputStream, productInfo, indentedOptions, token);
        }

        //await renderer.RenderAsync(Renderable.TextLine("Product info written to: " + productFile));
        foreach (var depot in productInfo.Depots)
        {
            //await renderer.RenderAsync(Renderable.TextLine("Depot: " + depot.DepotId));
            foreach (var (branch, manifestInfo) in depot.Manifests)
            {
                //await renderer.RenderAsync(Renderable.TextLine("Branch: " + branch));
                //await renderer.RenderAsync(Renderable.TextLine("Manifest: " + manifestInfo.ManifestId));
                
                var manifest = await steamSession.GetManifestContents(appId, depot.DepotId, manifestInfo.ManifestId, branch, token);
                
                var manifestPath = output / "stores" / "steam" / "manifests" / (manifest.ManifestId + ".json").ToRelativePath();
                {
                    manifestPath.Parent.CreateDirectory();
                    await using var outputStream = manifestPath.Create();
                    await JsonSerializer.SerializeAsync(outputStream, manifest, indentedOptions, token);
                }
                
                await IndexManifest(steamSession, renderer, appId, output, manifest, indentedOptions, existingHashes, token);
            }
        }

        return 0;
    }

    private static async Task<ConcurrentBag<Sha1>> LoadExistingHashes(AbsolutePath folder, JsonSerializerOptions options, CancellationToken token)
    {
        var bag = new ConcurrentBag<Sha1>();
        var hashFiles = folder.EnumerateFiles("*.json", true);
        
        await Parallel.ForEachAsync(hashFiles, token, async (file, token) =>
        {
            await using var stream = file.Read();
            var hash = await JsonSerializer.DeserializeAsync<MultiHash>(stream, options, token);
            bag.Add(hash!.Sha1);
        });

        return bag;
    }

    private static async Task IndexManifest(ISteamSession session, IRenderer renderer, AppId appId, AbsolutePath output, Manifest manifest, JsonSerializerOptions indentedOptions, ConcurrentBag<Sha1> existingHashes, CancellationToken token)
    {
        var writeLock = new SemaphoreSlim(1, 1);
        await Parallel.ForEachAsync(manifest.Files, token, async (file, token) =>
            {
                if (file.Size == Size.Zero)
                    return;
                if (existingHashes.Contains(file.Hash))
                    return;
                
                
                await renderer.RenderAsync(Renderable.TextLine("File: " + file.Path));
                await using var stream = session.GetFileStream(appId, manifest, file.Path);
                MultiHasher hasher = new();
                var multiHash = await hasher.HashStream(stream, token);

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
