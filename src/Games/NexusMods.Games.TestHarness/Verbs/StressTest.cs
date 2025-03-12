using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Games.AdvancedInstaller.UI;
using NexusMods.Hashing.xxHash3;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.StandardGameLocators;
using StrawberryShake;
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
        [Injected] INexusGraphQLClient nexusGqlClient,
        [Injected] IGameDomainToGameIdMappingCache domainToIdCache,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] ILibraryService libraryService,
        [Injected] IEnumerable<IGameLocator> gameLocators,
        [Injected] IGuidedInstaller optionSelector,
        [Injected] NexusModsLibrary nexusModsLibrary,
        [Injected] CancellationToken token)
    {
        var manualLocator = gameLocators.OfType<ManuallyAddedLocator>().First();
        AdvancedManualInstallerUI.Headless = true;

        var domain = (await domainToIdCache.TryGetDomainAsync(game.GameId, token)).Value.Value;
        var mods = await nexusApiClient.ModUpdatesAsync(domain, PastTime.Day, token);
        var results = new List<(string FileName, ModId ModId, Abstractions.NexusWebApi.Types.V2.FileId FileId, Hash Hash, bool Passed, Exception? exception)>();

        await using var gameFolder = temporaryFileManager.CreateFolder();
        var (manualId, install) = await manualLocator.Add(game, new Version(1, 0), gameFolder);

        try
        {
            foreach (var mod in mods.Data)
            {
                await renderer.Text("Processing {0}", mod.ModId);

                IOperationResult<IModFilesResult> files;
                try
                {
                    files = await nexusGqlClient.ModFiles.ExecuteAsync(mod.ModId.ToString(), game.GameId.ToString(), token);
                    files.EnsureNoErrors();
                }
                catch (HttpRequestException ex)
                {
                    await renderer.Error(ex, "Failed to get files for {0}", mod.ModId);
                    continue;
                }

                var hash = Hash.Zero;
                foreach (var file in files.Data!.ModFiles.Where(f => Size.FromLong(long.Parse(f.SizeInBytes ?? "0")) < MaxFileSize))
                {
                    var uid = UidForFile.FromV2Api(file.Uid);
                    try
                    {
                        var size = Size.FromLong(long.Parse(file.SizeInBytes ?? "0"));
                        await renderer.Text("Downloading {0} {1} {2} - {3}", mod.ModId,
                            uid.FileId,
                            file.Name,
                            size);


                        await using var tmpPath = temporaryFileManager.CreateFile();

                        var downloadJob = await nexusModsLibrary.CreateDownloadJob(tmpPath, game.GameId, mod.ModId, uid.FileId, cancellationToken: CancellationToken.None);
                        var libraryFile = await libraryService.AddDownload(downloadJob);

                        await renderer.Text("Installing {0} {1} {2} - {3}", mod.ModId,
                            uid.FileId,
                            file.Name,
                            size);

                        var list = await game.Synchronizer.CreateLoadout(install);

                        await libraryService.InstallItem(libraryFile.AsLibraryItem(), list.LoadoutId);
                        
                        results.Add((file.Name, mod.ModId, uid.FileId, hash, true, null));
                        await renderer.Text("Installed {0} {1} {2} - {3}", mod.ModId, uid.FileId,
                            file.Name, size);
                    }
                    catch (Exception ex)
                    {
                        await renderer.Error(ex, "Failed to install {0} {1} {2}", mod.ModId, uid.FileId,
                            file.Name);
                        results.Add((file.Name, mod.ModId, uid.FileId, hash, false, ex));
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
                $"# {domain} Test Results - {results.Count} files",
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
