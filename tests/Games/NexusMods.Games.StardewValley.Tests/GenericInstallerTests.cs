using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk.NexusModsApi;
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
    [Trait("RequiresApiKey", "True")]
    public async Task Test_NotSupported()
    {
        ApiKeyTestHelper.RequireApiKey();
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
        result.IsNotSupported(out var reason).Should().BeTrue();
        reason.Should().Be("The installer doesn't support putting files in the Content folder");
    }

    [Theory]
    [Trait("RequiresApiKey", "True")]
    [InlineData(1915, 124659,1)] // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
    [InlineData(16893, 123812,2)] // Romanceable Rasmodius Redux Revamped 1.8.55 (https://www.nexusmods.com/stardewvalley/mods/16893?tab=files)
    [InlineData(18144, 114038,1)] // Romanceable Rasmodia - RRRR Patch 1.1 (https://www.nexusmods.com/stardewvalley/mods/18144?tab=files)
    [InlineData(31167, 123427,0)] // Item Bags for Stardew Valley Expanded 1.0.0 (https://www.nexusmods.com/stardewvalley/mods/31167?tab=files)
    [InlineData(20414, 126173, 2)] // Portraits for Vendors 1.9.3 - Nyapu's Portraits (https://www.nexusmods.com/stardewvalley/mods/20414?tab=files)
    [InlineData(1536, 98230, 1)] // Mail Framework Mod 1.18.0 (https://www.nexusmods.com/stardewvalley/mods/1536?tab=files)
    public async Task Test_Mods(uint modId, uint fileId, int expectedManifestCount)
    {
        ApiKeyTestHelper.RequireApiKey();
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

        await VerifyGroup(libraryArchive, group).UseParameters(modId, fileId);
    }
}
