using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

[Trait("RequiresNetworking", "True")]
public class GenericInstallerTests : ALibraryArchiveInstallerTests<GenericInstallerTests, StardewValley>
{
    private readonly GenericInstaller _installer;

    public GenericInstallerTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _installer = ServiceProvider.GetRequiredService<GenericInstaller>();
    }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.14"));
    }

    [Fact]
    public async Task Test_NotSupported()
    {
        var loadout = await CreateLoadout();

        // 3D NPC Houses 1.0 https://www.nexusmods.com/stardewvalley/mods/763?tab=files
        var libraryArchive = await DownloadArchiveFromNexusMods(ModId.From(763), FileId.From(2874));

        using var tx = Connection.BeginTransaction();
        var group = new LoadoutItemGroup.New(tx, out var id)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, id)
            {
                Name = "Foo",
                LoadoutId = loadout,
            },
        };

        var result = await _installer.ExecuteAsync(libraryArchive, group, tx, loadout, CancellationToken.None);
        result.IsNotSupported.Should().BeTrue();
    }

    [Theory]
    [InlineData(1915, 124659,1)] // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
    [InlineData(16893, 123812,2)] // Romanceable Rasmodius Redux Revamped 1.8.55 (https://www.nexusmods.com/stardewvalley/mods/16893?tab=files)
    [InlineData(18144, 114038,1)] // Romanceable Rasmodia - RRRR Patch 1.1 (https://www.nexusmods.com/stardewvalley/mods/18144?tab=files)
    [InlineData(31167, 123427,0)] // Item Bags for Stardew Valley Expanded 1.0.0 (https://www.nexusmods.com/stardewvalley/mods/31167?tab=files)
    [InlineData(20414, 126173, 2)] // Portraits for Vendors 1.9.3 - Nyapu's Portraits (https://www.nexusmods.com/stardewvalley/mods/20414?tab=files)
    public async Task Test_Mods(uint modId, uint fileId, int expectedManifestCount)
    {
        var loadout = await CreateLoadout();

        var libraryArchive = await DownloadArchiveFromNexusMods(ModId.From(modId), FileId.From(fileId));

        var group = await Install(_installer, loadout, libraryArchive);
        var groupFiles = GetFiles(group).ToArray();

        groupFiles.Should().NotBeEmpty().And.AllSatisfy(file =>
        {
            var (_, locationId, path) = file.AsLoadoutItemWithTargetPath().TargetPath;
            locationId.Value.Should().Be(LocationId.Game.Value);
            path.TopParent.Should().Be(Constants.ModsFolder);
        });

        var numManifests = groupFiles.Count(static loadoutItem => SMAPIManifestLoadoutFile.Load(loadoutItem.Db, loadoutItem.Id).IsValid());
        numManifests.Should().Be(expectedManifestCount);

        await VerifyGroup(libraryArchive, group, modId, fileId);
    }

    private static IEnumerable<LoadoutFile.ReadOnly> GetFiles(LoadoutItemGroup.ReadOnly group)
    {
        foreach (var loadoutItem in group.Children)
        {
            loadoutItem.TryGetAsLoadoutItemWithTargetPath(out var targetPath).Should().BeTrue();
            targetPath.IsValid().Should().BeTrue();

            targetPath.TryGetAsLoadoutFile(out var loadoutFile).Should().BeTrue();
            loadoutFile.IsValid().Should().BeTrue();

            yield return loadoutFile;
        }
    }

    private static async Task VerifyGroup(LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.ReadOnly group, uint modId, uint fileId, [CallerFilePath] string sourceFile = "")
    {
        var sb = new StringBuilder();

        var paths = GetFiles(group)
            .Select(file =>
            {
                libraryArchive.Children
                    .Where(x => x.AsLibraryFile().Hash == file.Hash)
                    .TryGetFirst(x => x.Path.FileName == file.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName, out var libraryArchiveFileEntry)
                    .Should().BeTrue();

                libraryArchiveFileEntry.IsValid().Should().BeTrue();
                return (libraryArchiveFileEntry, file.AsLoadoutItemWithTargetPath().TargetPath);
            })
            .OrderBy(static targetPath => targetPath.Item2.Item2)
            .ThenBy(static targetPath => targetPath.Item2.Item3)
            .ToArray();

        foreach (var tuple in paths)
        {
            var (libraryArchiveFileEntry, targetPath) = tuple;
            var (_, locationId, path) = targetPath;
            var gamePath = new GamePath(locationId, path);

            sb.AppendLine($"{libraryArchiveFileEntry.AsLibraryFile().Hash}: {libraryArchiveFileEntry.Path} -> {gamePath}");
        }

        var result = sb.ToString();

        // ReSharper disable once ExplicitCallerInfoArgument
        await Verify(result, sourceFile: sourceFile).UseParameters(modId, fileId);
    }
}
