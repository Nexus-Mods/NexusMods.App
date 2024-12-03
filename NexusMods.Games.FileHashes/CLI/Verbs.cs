using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Games.FileHashes.DTO;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
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
        [Option("o", "outputFolder", "The folder to save the hashes to")] AbsolutePath outputFolder)
    {
        foreach (var os in inputFolder.EnumerateDirectories(recursive: false))
        {
            foreach (var version in os.EnumerateDirectories(recursive: false))
            {
                List<GameFileHashes> hashes = [];
                foreach (var file in version.EnumerateFiles())
                {
                    await renderer.RenderAsync(Renderable.Text($"Hashing {file}"));
                    await using var fileStream = file.Read();
                    hashes.Add(await Utils.HashFile(fileStream, file.RelativeTo(version)));
                }

                var outputFile = outputFolder / $"{version.FileName}_{os.FileName}.json";
                outputFile.Parent.CreateDirectory();
                await using var outputStream = outputFile.Create();
                var indentedOptions = new JsonSerializerOptions(jsonOptions)
                {
                    WriteIndented = true,
                };
                await JsonSerializer.SerializeAsync(outputStream, hashes, indentedOptions);
            }
        }
        return 0;
    }
    
}
