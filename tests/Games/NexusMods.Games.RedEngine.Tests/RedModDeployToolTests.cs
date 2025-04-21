using System.Reactive.Linq;
using System.Text;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
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
            ISortableItem[] sortableItemsSnapshot = [];
            
            // listen for the order to be updated
            using var _ = provider.SortableItemsChangeSet
                .QueryWhenChanged(items =>
                    {
                        if (items.Count == 12 && !tsc1.Task.IsCompleted)
                        {
                            tsc1.SetResult(Unit.Default);
                            sortableItemsSnapshot = items.Items.ToArray();
                            return true;
                        }

                        return false;
                    }
                )
                .Subscribe();
            
        
            // NOTE(Al12rs): Correctness of test depends also on order of mods added to the loadout,
            // e.g. if RedMods are added one by one rather than in batch, that can affect the order.
            loadout = await AddRedMods(loadout);
        
            // wait for the order to be updated, but avoid stalling
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            if (await Task.WhenAny(tsc1.Task, timeoutTask) == timeoutTask)
            {
                provider.SortableItems.Count.Should().Be(12, because: "SortableItems should have been updated after the mods were installed");
                throw new TimeoutException($"Timed out waiting for SortableItems to be updated to contain 12 items, current count: {provider.SortableItems.Count}");
            }
            loadout = loadout.Rebase();
            
            var sb = new StringBuilder();
            sb.AppendLine("Starting Order:");
            sb.AppendLineN();
            sb.Append(AddLineNumbers(await WriteLoadoutFile(loadout)));
            sb.AppendLineN();
            sb.AppendLine($"Moved Item:");
            sb.AppendLine(name);
            sb.AppendLine($"Delta: {delta}");

            var specificRedMod = sortableItemsSnapshot.OfType<RedModSortableItem>().Single(g => g.DisplayName == name);

            var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = cts2.Token;
            var setPositionTask = provider.SetRelativePosition(specificRedMod, delta, token);
            var timeoutTask2 = Task.Delay(TimeSpan.FromSeconds(30));
            if (await Task.WhenAny(setPositionTask, timeoutTask2) == timeoutTask2)
            {
                throw new TimeoutException($"Timed out waiting for SetRelativePosition to complete");
            }

            loadout = loadout.Rebase();
            sb.AppendLine("After Move:");
            sb.AppendLineN();
            sb.Append(AddLineNumbers(await WriteLoadoutFile(loadout)));
            
            await Verify(sb.ToString()).UseParameters(name, delta);
        } catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.ToString());
            throw;
        }
    }
    
    
    [Theory]
    [InlineData(new[] { 0 }, 3, TargetRelativePosition.BeforeTarget)]
    [InlineData(new[] { 0, 1, 2 }, 11, TargetRelativePosition.AfterTarget)]
    [InlineData(new[] { 0, 11, 2 }, 5, TargetRelativePosition.AfterTarget)]
    [InlineData(new[] { 0, 5, 11 }, 5, TargetRelativePosition.BeforeTarget)]
    [InlineData(new[] { 10, 11 }, 0, TargetRelativePosition.BeforeTarget)]
    [InlineData(new[] { 10, 11 }, 0, TargetRelativePosition.AfterTarget)]
    public async Task MoveItemsMethodShouldWork(int[] sourceIndices, int targetIndex, TargetRelativePosition position)
    {
        var loadout = await CreateLoadout();
        
        var factory = ServiceProvider.GetRequiredService<RedModSortableItemProviderFactory>();
        
        // Wait for the factory to pick up the loadouts
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        var provider = factory.GetLoadoutSortableItemProvider(loadout);
        
        var tsc1 = new TaskCompletionSource<Unit>();
        ISortableItem[] sortableItemsSnapshot = [];

        // listen for the order to be updated
        using var _ = provider.SortableItemsChangeSet
            .QueryWhenChanged(items =>
                {
                    if (items.Count == 12 && !tsc1.Task.IsCompleted)
                    {
                        tsc1.SetResult(Unit.Default);
                        sortableItemsSnapshot = items.Items.ToArray();
                        return true;
                    }

                    return false;
                }
            )
            .Subscribe();
        
        // NOTE(Al12rs): Correctness of test depends also on order of mods added to the loadout,
        // e.g. if RedMods are added one by one rather than in batch, that can affect the order.
        loadout = await AddRedMods(loadout);
        
        // wait for the order to be updated, but avoid stalling
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
        if (await Task.WhenAny(tsc1.Task, timeoutTask) == timeoutTask)
        {
            provider.SortableItems.Count.Should().Be(12, because: "SortableItems should have been updated after the mods were installed");
            
            throw new TimeoutException($"Timed out waiting for SortableItems to be updated to contain 12 items, current count: {provider.SortableItems.Count}");
        }
        loadout = loadout.Rebase();
        
        var sourceItems = sortableItemsSnapshot.Where(item => sourceIndices.Contains(item.SortIndex)).ToArray();
        var targetItem = sortableItemsSnapshot.Single(g => g.SortIndex == targetIndex);
        
        var sb = new StringBuilder();
        sb.AppendLine("Starting Order:");
        sb.AppendLineN();
        sb.Append(AddLineNumbers(await WriteLoadoutFile(loadout)));
        sb.AppendLineN();
        sb.AppendLine($"Moved Indices:");
        sb.AppendLine($"[{string.Join(",", sourceIndices)}]");
        sb.AppendLine($"TargetItem: {targetIndex}: {targetItem.DisplayName}, position: {position}");
        
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cts2.Token;
        
        var moveItemsTask = provider.MoveItemsTo(sourceItems, targetItem, position, token);
        var timeoutTask2 = Task.Delay(TimeSpan.FromSeconds(30));
        if (await Task.WhenAny(moveItemsTask, timeoutTask2) == timeoutTask2)
        {
            throw new TimeoutException($"Timed out waiting for MoveItemsTo to complete");
        }
        
        loadout = loadout.Rebase();
        sb.AppendLine("After Move:");
        sb.AppendLineN();
        sb.Append(AddLineNumbers(await WriteLoadoutFile(loadout)));
        
        await Verify(sb.ToString()).UseParameters(sourceIndices, targetIndex, position);
    }
    
    internal async Task<string> WriteLoadoutFile(Loadout.ReadOnly loadout)
    {
        await using var tempFile = TemporaryFileManager.CreateFile();
        loadout = loadout.Rebase();
        await _tool.WriteLoadOrderFile(tempFile.Path, loadout);
        return await tempFile.Path.ReadAllTextAsync();
    }
    
    internal string AddLineNumbers(string text)
    {
        var textSpan = text.AsSpan();
        var sb = new StringBuilder();
        var index = 0;
        foreach (var line  in textSpan.EnumerateLines())
        {
            if (line.IsWhiteSpace())
                continue;
            sb.AppendLine($"{index}: {line}");
            index++;
        }

        return sb.ToString();
    }

    internal async Task<Loadout.ReadOnly> AddRedMods(Loadout.ReadOnly loadout)
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
