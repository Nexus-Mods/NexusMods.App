using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.StandardGameLocators;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.ModId;

namespace NexusMods.Games.TestHarness.Verbs;

public class StressTest
{
    private static readonly Size MaxFileSize = Size.GB;

    [Verb("stress-test", "Stress test the application by installing all recent mods for a given game")]
    internal static async Task<int> RunStressTest(
        [Injected] IRenderer renderer,
        [Option("g", "game", "The game to test")] IGame game,
        [Option("o", "output", "Output path for the resulting report")] AbsolutePath output,
        [Injected] INexusApiClient nexusApiClient,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] ILoadoutRegistry loadoutRegistry,
        [Injected] IHttpDownloader downloader,
        [Injected] IArchiveInstaller archiveInstaller,
        [Injected] IFileOriginRegistry fileOriginRegistry,
        [Injected] IEnumerable<IGameLocator> gameLocators,
        [Injected] IGuidedInstaller optionSelector,
        [Injected] CancellationToken token)
    {
        var manualLocator = gameLocators.OfType<ManuallyAddedLocator>().First();

        var mods = await nexusApiClient.ModUpdatesAsync(game.Domain.Value, PastTime.Day, token);
        var results = new List<(string FileName, ModId ModId, Abstractions.NexusWebApi.Types.FileId FileId, Hash Hash, bool Passed, Exception? exception)>();

        await using var gameFolder = temporaryFileManager.CreateFolder();
        var gameId = manualLocator.Add(game, new Version(1, 0), gameFolder);
        game.ResetInstallations();
        var install = game.Installations.First(f => f.Store == GameStore.ManuallyAdded);

        try
        {
            foreach (var mod in mods.Data)
            {
                await renderer.Text("Processing {ModId}", mod.ModId);

                Response<ModFiles> files;
                try
                {
                    files = await nexusApiClient.ModFilesAsync(game.Domain.Value, mod.ModId, token);
                }
                catch (HttpRequestException ex)
                {
                    await renderer.Error(ex, "Failed to get files for {ModId}", mod.ModId);
                    continue;
                }

                var hash = Hash.Zero;
                foreach (var file in files.Data.Files.Where(f => Size.FromLong(f.SizeInBytes ?? 0) < MaxFileSize))
                {
                    try
                    {
                        if (file.CategoryId != 1) continue;
                        await renderer.Text("Downloading {ModId} {FileId} {FileName} - {Size}", mod.ModId,
                            file.FileId,
                            file.FileName,
                            Size.FromLong(file.SizeInBytes ?? 0));

                        var urls = await nexusApiClient.DownloadLinksAsync(game.Domain.Value, mod.ModId, file.FileId, token);
                        await using var tmpPath = temporaryFileManager.CreateFile();

                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromMinutes(20));

                        hash = await downloader.DownloadAsync(urls.Data.Select(d => d.Uri), tmpPath, token: cts.Token);

                        await renderer.Text("Installing {ModId} {FileId} {FileName} - {Size}", mod.ModId,
                            file.FileId,
                            file.FileName,
                            Size.FromLong(file.SizeInBytes ?? 0));

                        var list = await loadoutRegistry.Manage(install);
                        var downloadId = await fileOriginRegistry.RegisterDownload(tmpPath, new FilePathMetadata
                            { OriginalName = tmpPath.Path.Name, Quality = Quality.Low }, token);
                        await archiveInstaller.AddMods(list.Value.LoadoutId, downloadId, token: token);

                        results.Add((file.FileName, mod.ModId, file.FileId, hash, true, null));
                        await renderer.Text("Installed {ModId} {FileId} {FileName} - {Size}", mod.ModId, file.FileId,
                            file.FileName, Size.FromLong(file.SizeInBytes ?? 0));
                    }
                    catch (Exception ex)
                    {
                        await renderer.Error(ex, "Failed to install {ModId} {FileId} {FileName}", mod.ModId, file.FileId,
                            file.FileName);
                        results.Add((file.FileName, mod.ModId, file.FileId, hash, false, ex));
                    }

                }
            }

            await renderer.Table(new[] { "Name", "ModId", "FileId", "Hash", "Passed", "Exception" },
                results.Select(r => new object[]
                {
                    r.FileName, r.ModId.ToString(), r.FileId.ToString(), r.Hash, r.Passed.ToString(),
                    r.exception?.Message ?? ""
                }));

            var lines = new List<string>
            {
                $"# {game.Domain} Test Results - {results.Count} files",
                "",
                "| Status | Name | ModId | FileId | Hash | Exception |",
                "| ---- | ----- | ------ | ---- | ------ | --------- |"
            };
            foreach (var result in results.OrderBy(x => x.Passed)
                         .ThenBy(x => x.ModId)
                         .ThenBy(x => x.FileId))
            {
                var status = result.Passed ? ":white_check_mark:" : ":x:";
                lines.Add(
                    $"| {status} | {result.FileName} | {result.ModId} | {result.FileId} | {result.Hash} | {result.exception?.Message} |");
            }

            if (output != default)
                await output.WriteAllLinesAsync(lines, token);

        }
        finally
        {
            manualLocator.Remove(gameId);
        }

        return 0;
    }
}
