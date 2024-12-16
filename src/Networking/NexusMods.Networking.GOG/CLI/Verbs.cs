using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.OAuth;
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
        [Injected] CancellationToken token)
    {
        
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        foreach (var os in Enum.GetValues<OS>())
        {

            var builds = await client.GetBuilds(productId, OS.windows, token);

            foreach (var build in builds)
            {
                var buildPath = output / "stores" / "GOG" / "builds" / (build.BuildId + ".json");
                buildPath.Parent.CreateDirectory();
                {
                    buildPath.Parent.CreateDirectory();
                    await using var outputStream = buildPath.Create();
                    await JsonSerializer.SerializeAsync(outputStream, build, indentedOptions, token);
                }
                
                var depot = await client.GetDepot(build, token);
                
                var depotPath = output / "stores" / "GOG" / "depots" / (build.BuildId + ".json");
                depotPath.Parent.CreateDirectory();
                {
                    depotPath.Parent.CreateDirectory();
                    await using var outputStream = depotPath.Create();
                    await JsonSerializer.SerializeAsync(outputStream, depot, indentedOptions, token);
                }

                var hashPathRoot = output / "hashes";
                hashPathRoot.CreateDirectory();
                
                var hashLock = new SemaphoreSlim(1, 1);
                
                await Parallel.ForEachAsync(depot.Items, token, async (item, cancellationToken) =>
                    {
                        Console.WriteLine($"  {item.Path}");

                        
                        await using var stream = await client.GetFileStream(build, depot, item.Path, token);
                        var multiHasher = new MultiHasher();
                        var multiHash = await multiHasher.HashStream(stream, token);

                        var hashStr = multiHash.XxHash3.ToString()[2..];
                        var path = hashPathRoot / $"{hashStr[..2]}" / (hashStr.ToRelativePath() + ".json");
                        
                        path.Parent.CreateDirectory();
                        await hashLock.WaitAsync(token);
                        
                        try {
                            await using var outputStream = path.Create(); 
                            await JsonSerializer.SerializeAsync(outputStream, multiHash, indentedOptions, token);
                        } finally {
                            hashLock.Release();
                        }
                    }
                );
            }
            
            /*
            var build = builds.First();
            var depot = await client.GetDepot(builds.First(), token);

            await using var stream = await client.GetFileStream(build, depot, "System.Formats.Asn1.dll",
                token
            );
            var multiHash = new MultiHasher();
            var hash = await multiHash.HashStream(stream, token);
            */
        }

        return 0;
    }
}
