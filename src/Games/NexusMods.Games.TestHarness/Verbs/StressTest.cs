using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.StandardGameLocators;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.V2.ModId;

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
        [Injected] IHttpDownloader downloader,
        [Injected] ILibraryService libraryService,
        [Injected] IEnumerable<IGameLocator> gameLocators,
        [Injected] IGuidedInstaller optionSelector,
        [Injected] CancellationToken token)
    {
        var manualLocator = gameLocators.OfType<ManuallyAddedLocator>().First();
        AdvancedManualInstallerUI.Headless = true;

        var mods = await nexusApiClient.ModUpdatesAsync(game.Domain.Value, PastTime.Day, token);
        var results = new List<(string FileName, ModId ModId, Abstractions.NexusWebApi.Types.V2.FileId FileId, Hash Hash, bool Passed, Exception? exception)>();

        await using var gameFolder = temporaryFileManager.CreateFolder();
        var (manualId, install) = await manualLocator.Add(game, new Version(1, 0), gameFolder);

        try
        {
            foreach (var mod in mods.Data)
            {
                await renderer.Text("Processing {0}", mod.ModId);

                Response<ModFiles> files;
                try
                {
                    files = await nexusApiClient.ModFilesAsync(game.Domain.Value, mod.ModId, token);
                }
                catch (HttpRequestException ex)
                {
                    await renderer.Error(ex, "Failed to get files for {0}", mod.ModId);
                    continue;
                }

                var hash = Hash.Zero;
                foreach (var file in files.Data.Files.Where(f => Size.FromLong(f.SizeInBytes ?? 0) < MaxFileSize))
                {
                    try
                    {
                        if (file.CategoryId != 1) continue;
                        await renderer.Text("Downloading {0} {1} {2} - {3}", mod.ModId,
                            file.FileId,
                            file.FileName,
                            Size.FromLong(file.SizeInBytes ?? 0));

                        var urls = await nexusApiClient.DownloadLinksAsync(game.Domain.Value, mod.ModId, file.FileId, token);
                        await using var tmpPath = temporaryFileManager.CreateFile();

                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromMinutes(20));

                        hash = await downloader.DownloadAsync(urls.Data.Select(d => d.Uri), tmpPath, token: cts.Token);

                        await renderer.Text("Installing {0} {1} {2} - {3}", mod.ModId,
                            file.FileId,
                            file.FileName,
                            Size.FromLong(file.SizeInBytes ?? 0));

                        var list = await game.Synchronizer.CreateLoadout(install);

                        var localFile = await libraryService.AddLocalFile(tmpPath);
                        await libraryService.InstallItem(localFile.AsLibraryFile().AsLibraryItem(), list.LoadoutId);
                        
                        results.Add((file.FileName, mod.ModId, file.FileId, hash, true, null));
                        await renderer.Text("Installed {0} {1} {2} - {3}", mod.ModId, file.FileId,
                            file.FileName, Size.FromLong(file.SizeInBytes ?? 0));
                    }
                    catch (Exception ex)
                    {
                        await renderer.Error(ex, "Failed to install {0} {1} {2}", mod.ModId, file.FileId,
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
            await manualLocator.Remove(manualId);
        }

        return 0;
    }
}
