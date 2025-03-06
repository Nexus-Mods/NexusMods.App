using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using R3;
using ReactiveUI;
using Xunit.Abstractions;

namespace NexusMods.Games.RedEngine.Tests;

public class RedModDeployToolTests : ACyberpunkIsolatedGameTest<Cyberpunk2077Game>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly RedModDeployTool _tool;

    public RedModDeployToolTests(ITestOutputHelper helper, ITestOutputHelper testOutputHelper) : base(helper)
    {
        _testOutputHelper = testOutputHelper;
        _tool = ServiceProvider.GetServices<ITool>().OfType<RedModDeployTool>().Single();
    }

    [Fact]
    public async Task LoadorderFileIsWrittenCorrectly()
    {
        var loadout = await CreateLoadout();
        loadout = await AddRedMods(loadout);
        await Verify(await WriteLoadoutFile(loadout));
    }

    [Theory]
    [InlineData("Driver_Shotguns", 0)]
    [InlineData("Driver_Shotguns", 3)]
    [InlineData("Driver_Shotguns", -3)]
    [InlineData("Driver_Shotguns", -11)]
    [InlineData("maestros_of_synth_body_heat_radio", -1)]
    [InlineData("maestros_of_synth_body_heat_radio", 10)]
    [InlineData("maestros_of_synth_the_dirge", -11)]
    public async Task MovingModsRelativelyResultsInCorrectOrdering(string name, int delta)
    {
        try
        {
            var loadout = await CreateLoadout();

            var factory = ServiceProvider.GetRequiredService<RedModSortableItemProviderFactory>();
            // Wait for the factory to pick up the loadouts
            await Task.Delay(TimeSpan.FromSeconds(1));
        
            var provider = factory.GetLoadoutSortableItemProvider(loadout);

            var tsc1 = new TaskCompletionSource<Unit>();
            // avoid stalling the test on failure
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tsc1.TrySetCanceled(), useSynchronizationContext: false);

            // listen for the order to be updated
            using var _ = provider.SortableItems
                .WhenAnyValue(coll => coll.Count)
                .Where(count => count == 12)
                .Distinct()
                .Subscribe(_ =>
                    {
                        if (!tsc1.Task.IsCompleted)
                        {
                            tsc1.SetResult(Unit.Default);
                        }
                    }
                );
        
            // NOTE(Al12rs): Correctness of test depends also on order of mods added to the loadout,
            // e.g. if RedMods are added one by one rather than in batch, that can affect the order.
            loadout = await AddRedMods(loadout);
        
            // wait for the order to be updated
            await tsc1.Task;

            var order = provider.SortableItems;
            var specificRedMod = order.OfType<RedModSortableItem>().Single(g => g.DisplayName == name);

            var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = cts2.Token;
            await provider.SetRelativePosition(specificRedMod, delta, token);

            loadout = loadout.Rebase();

            await Verify(await WriteLoadoutFile(loadout)).UseParameters(name, delta);
        } catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
    }


    private async Task<string> WriteLoadoutFile(Loadout.ReadOnly loadout)
    {
        await using var tempFile = TemporaryFileManager.CreateFile();
        loadout = loadout.Rebase();
        await _tool.WriteLoadOrderFile(tempFile.Path, loadout);
        return await tempFile.Path.ReadAllTextAsync();
    }

    private async Task<Loadout.ReadOnly> AddRedMods(Loadout.ReadOnly loadout)
    {
        var files = new[] { "one_mod.7z", "several_red_mods.7z" };

        await using var tempDir = TemporaryFileManager.CreateFolder();
        foreach (var file in files)
        {
            var sourcePath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("LibraryArchiveInstallerTests/Resources/" + file);
            var copyPath = tempDir.Path.Combine(file);
            // Create copy to avoid "file in use" by other tests issues
            File.Copy(sourcePath.ToString(), copyPath.ToString(), overwrite: true);

            var libraryArchive = await RegisterLocalArchive(copyPath);
            await LibraryService.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout);
        }

        loadout = loadout.Rebase();
        return loadout;
    }
}
