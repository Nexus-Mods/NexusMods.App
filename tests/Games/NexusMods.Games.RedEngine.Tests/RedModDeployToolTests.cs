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
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using OneOf;
using R3;
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
        // Ensure the SortOrderManager is created and subscribed to loadout changes
        _ = InitAndGetSortOrderManager(); 
        var loadout = await CreateLoadout();
        loadout = await AddRedMods(loadout);
        await Verify(await WriteLoadoutFile(loadout));
    }
    
    [Fact]
    public async Task LoadorderFileDoesNotContainDisabledMods()
    {
        // Ensure the SortOrderManager is created and subscribed to loadout changes
        _ = InitAndGetSortOrderManager();
        var redModSortOrderVariety = ServiceProvider.GetRequiredService<RedModSortOrderVariety>();
        var loadout = await CreateLoadout();
        
        loadout = await AddRedMods(loadout);
        await Task.Delay(TimeSpan.FromSeconds(2));

        var optionalSortOrderId = redModSortOrderVariety.GetSortOrderIdFor(loadout.LoadoutId);
        optionalSortOrderId.HasValue.Should().BeTrue("RedModSortOrderVariety should have a SortOrderId for the loadout");
        var sortOrderId = optionalSortOrderId.Value;
        
        var sortOrder = redModSortOrderVariety.GetSortOrderItems(sortOrderId, Connection.Db);
        
        var driverMod = sortOrder.Single(g => g.DisplayName == "Driver_Shotguns").ModGroupId;
        driverMod.HasValue.Should().BeTrue("Driver_Shotguns mod should have a ModGroupId");
        var driverModGroupId = driverMod.Value;
        
        // Find the Driver_Shotguns mod
        var modToDisable = LoadoutItemGroup.All(Connection.Db).Single(g => g.LoadoutItemGroupId == driverModGroupId);
        
        // Disable the mod
        var tx = Connection.BeginTransaction();
        tx.Add(modToDisable.Id, LoadoutItem.Disabled, Null.Instance);
        await tx.Commit();
        
        // Give some time for the SortOrderManager to process the change
        await Task.Delay(TimeSpan.FromSeconds(2)); 
        
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
            
            // Ensure the SortOrderManager is created and subscribed to loadout changes
            var sortOrderManager = InitAndGetSortOrderManager();
            
            var redModSortOrderVariety = ServiceProvider.GetRequiredService<RedModSortOrderVariety>();
        
            // NOTE(Al12rs): Correctness of test depends also on order of mods added to the loadout,
            // e.g. if RedMods are added one by one rather than in batch, that can affect the order.
            loadout = await AddRedMods(loadout);

            // create a cancellation token that will time out after 30 seconds
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            // ensure sort order is updated with the latest loadout changes
            await sortOrderManager.UpdateLoadOrders(loadout.LoadoutId, token: cts1.Token);
            
            loadout = loadout.Rebase();
            
            var optionalSortOrderId = redModSortOrderVariety.GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId>.FromT0(loadout.LoadoutId), Connection.Db);
            optionalSortOrderId.HasValue.Should().BeTrue("RedModSortOrderVariety should have a SortOrderId for the loadout");
            
            var sortOrderId = optionalSortOrderId.Value;
            var sortOrder = redModSortOrderVariety.GetSortOrderItems(sortOrderId, Connection.Db);
            
            loadout = loadout.Rebase();
            
            var sb = new StringBuilder();
            sb.AppendLine("Starting Order:");
            sb.AppendLineN();
            sb.Append(AddLineNumbers(await WriteLoadoutFile(loadout)));
            sb.AppendLineN();
            sb.AppendLine($"Moved Item:");
            sb.AppendLine(name);
            sb.AppendLine($"Delta: {delta}");
            
            var specificRedMod = sortOrder.Single(g => g.DisplayName == name);
    
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var setPositionTask = redModSortOrderVariety.MoveItemDelta(sortOrderId, specificRedMod.Key, delta, token: cts2.Token);
            
            
            var timeoutTask2 = Task.Delay(TimeSpan.FromSeconds(30));
            // TODO: Change MoveItemDelta to task
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
        
        // Ensure the SortOrderManager is created and subscribed to loadout changes
        var sortOrderManager = InitAndGetSortOrderManager();
            
        var redModSortOrderVariety = ServiceProvider.GetRequiredService<RedModSortOrderVariety>();
        
        // NOTE(Al12rs): Correctness of test depends also on order of mods added to the loadout,
        // e.g. if RedMods are added one by one rather than in batch, that can affect the order.
        loadout = await AddRedMods(loadout);
        
        // create a cancellation token that will time out after 30 seconds
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        // ensure sort order is updated with the latest loadout changes
        await sortOrderManager.UpdateLoadOrders(loadout.LoadoutId, token: cts1.Token);
        
        loadout = loadout.Rebase();
            
        var optionalSortOrderId = redModSortOrderVariety.GetSortOrderIdFor(OneOf<LoadoutId, CollectionGroupId>.FromT0(loadout.LoadoutId), Connection.Db);
        optionalSortOrderId.HasValue.Should().BeTrue("RedModSortOrderVariety should have a SortOrderId for the loadout");
        
        var sortOrderId = optionalSortOrderId.Value;
        var sortOrder = redModSortOrderVariety.GetSortOrderItems(sortOrderId, Connection.Db);
        
        var sourceItems = sortOrder.Where(item => sourceIndices.Contains(item.SortIndex)).ToArray();
        var targetItem = sortOrder.Single(g => g.SortIndex == targetIndex);
        
        loadout = loadout.Rebase();
        
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
        
        var moveItemsTask = redModSortOrderVariety.MoveItems(sortOrderId, sourceItems.Select(item => item.Key).ToArray(), targetItem.Key, position, token: token);
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
            var result = await LoadoutManager.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout);
        }

        loadout = loadout.Rebase();
        return loadout;
    }
}
