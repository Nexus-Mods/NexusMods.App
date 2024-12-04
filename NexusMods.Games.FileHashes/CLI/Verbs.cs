using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.FileHashes.DTO;
using NexusMods.Games.FileHashes.HashValues;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Games.FileHashes.CLI;

internal static class Verbs
{
    internal static IServiceCollection AddFileHashesVerbs(this IServiceCollection collection) =>
        collection.AddVerb(() => HashGameFolders);


    [Verb("hash-game-folders", "Hashes the files in a game folder and saves the hashes to a file.")]
    private static async Task<int> HashGameFolders([Injected] IRenderer renderer,
        [Injected] IGameRegistry gameRegistry,
        [Injected] JsonSerializerOptions jsonOptions,
        [Option("i", "inputFolder", "Games to index in the format of {inputFolder}/{os}/{version}/")] AbsolutePath inputFolder,
        [Option("g", "gameDomain", "The domain of the game")] string gameDomainName,
        [Option("o", "outputFolder", "The folder to save the hashes to")] AbsolutePath outputFolder)
    {
        var gameDomain = GameDomain.From(gameDomainName);
        foreach (var store in inputFolder.EnumerateDirectories(recursive: false))
        {
            var storeName = GameStore.From(store.FileName);
            foreach (var os in store.EnumerateDirectories(recursive: false))
            {
                var osEnum = Enum.Parse<OSType>(os.FileName, ignoreCase: true);
                await HashGameFiles(renderer, jsonOptions, storeName, outputFolder,
                    os, gameDomain, storeName,
                    osEnum
                );
            }
        }
        return 0;
    }

    private static async Task HashGameFiles(IRenderer renderer, JsonSerializerOptions jsonOptions, GameStore gameStore, AbsolutePath outputFolder, AbsolutePath os, GameDomain gameDomain, GameStore storeName, OSType osEnum)
    {
        foreach (var version in os.EnumerateDirectories(recursive: false))
        {
            var versionParsed = Version.Parse(version.FileName);
            List<GameFileHashes> hashes = [];
            foreach (var file in version.EnumerateFiles())
            {
                await renderer.RenderAsync(Renderable.Text($"Hashing {file}"));
                await using var fileStream = file.Read();
                var record = new GameFileHashes
                {
                    Path = file.RelativeTo(version),
                    XxHash3 = await fileStream.XxHash3Async(CancellationToken.None),
                    MinimalHash = await fileStream.MinimalHash(CancellationToken.None),
                    Sha1 = await fileStream.Sha1HashAsync(),
                    Md5 = await fileStream.Md5HashAsync(),
                    Domain = gameDomain,
                    Store = gameStore,
                    Version = versionParsed,
                    OS = osEnum,
                    Size = Size.FromLong(fileStream.Length),
                };
                hashes.Add(record);
            }

            var outputFile = outputFolder / $"{gameDomain}_{storeName}_{version.FileName}_{os.FileName}.json";
            outputFile.Parent.CreateDirectory();
            await using var outputStream = outputFile.Create();
            var indentedOptions = new JsonSerializerOptions(jsonOptions)
            {
                WriteIndented = true,
            };
            await JsonSerializer.SerializeAsync(outputStream, hashes, indentedOptions);
        }
    }
}
