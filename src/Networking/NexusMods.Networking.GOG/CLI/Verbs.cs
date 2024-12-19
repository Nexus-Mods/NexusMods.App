using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.OAuth;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.GOG.CLI;

public static class Verbs
{
    internal static IServiceCollection AddGOGVerbs(this IServiceCollection collection) =>
        collection
            .AddVerb(() => Login)
            .AddVerb(() => Index);

    [Verb("gog-login", "Indexes a Steam app and updates the given output folder")]
    private static async Task<int> Login([Injected] Client client)
    {
        await client.Login(CancellationToken.None);
        return 0;
    }
    
    [Verb("index-gog-app", "Indexes a GOG app and updates the given output folder")]
    private static async Task<int> Index(
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] Client client, 
        [Option("p", "productId", "The GOG product ID to get the product info of")] ProductId productId,
        [Option("o", "output", "The output folder to write the index to")] AbsolutePath output,
        [Option("v", "verify", "Verify the files are hashed properly. This takes longer to execute but ensures the files are downloaded correctly.")] bool verify,
        [Injected] CancellationToken token)
    {
        
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        foreach (var os in Enum.GetValues<OS>())
        {

            var builds = await client.GetBuilds(productId, os, token);

            foreach (var build in builds)
            {
                var buildPath = output / "stores" / "gog" / "builds" / (build.BuildId + ".json");
                buildPath.Parent.CreateDirectory();
                {
                    buildPath.Parent.CreateDirectory();
                    await using var outputStream = buildPath.Create();
                    await JsonSerializer.SerializeAsync(outputStream, build, indentedOptions, token);
                }
                
                var depot = await client.GetDepot(build, token);
                
                var depotPath = output / "stores" / "gog" / "depots" / (build.BuildId + ".json");
                
                if (depotPath.FileExists)
                    continue;
                
                depotPath.Parent.CreateDirectory();
                {
                    depotPath.Parent.CreateDirectory();
                    await using var outputStream = depotPath.Create();
                    await JsonSerializer.SerializeAsync(outputStream, depot, indentedOptions, token);
                }

                var hashPathRoot = output / "hashes";
                hashPathRoot.CreateDirectory();
                
                var hashLock = new SemaphoreSlim(1, 1);
                
                ConcurrentDictionary<string, Hash> foundHashes = new();
                
                await Parallel.ForEachAsync(depot.Items, token, async (item, token) =>
                    {
                        Console.WriteLine($"  {item.Path}");

                        
                        await using var stream = await client.GetFileStream(build, depot, item.Path, token);
                        var multiHasher = new MultiHasher();
                        var multiHash = await multiHasher.HashStream(stream, token);

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
                                await JsonSerializer.SerializeAsync(outputStream, multiHash, indentedOptions, token);
                            }
                            finally
                            {
                                hashLock.Release();
                            }
                        }

                        if (verify)
                        {
                            var offset = Size.Zero;
                            foreach (var chunk in item.Chunks)
                            {
                                stream.Position = (long)offset.Value;
                                
                                using var rented = MemoryPool<byte>.Shared.Rent((int)chunk.Size.Value);
                                var sized = rented.Memory[..(int)chunk.Size.Value];
                                await stream.ReadExactlyAsync(sized, token);
                                
                                var md5 = Md5.From(MD5.HashData(sized.Span));
                                if (!md5.Equals(chunk.Md5))
                                    throw new InvalidOperationException("MD5 mismatch");
                                offset += chunk.Size;
                            }
                        }
                    }
                );
                
                var foundHashPath = output / "stores" / "gog" / "found_hashes" / (build.BuildId + ".json");
                foundHashPath.Parent.CreateDirectory();
                await using var foundHashStream = foundHashPath.Create();
                await JsonSerializer.SerializeAsync(foundHashStream, foundHashes, indentedOptions, token);
            }
        }

        return 0;
    }
}
