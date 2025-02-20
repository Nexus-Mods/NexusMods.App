using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

[Trait("RequiresNetworking", "True")]
public class ModOverwritesGameFilesEmitterTests : ALoadoutDiagnosticEmitterTest<ModOverwritesGameFilesEmitterTests, StardewValley, ModOverwritesGameFilesEmitter>
{
    public ModOverwritesGameFilesEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.14"))
            .AddSingleton<DummyInstaller>();
    }

    [Fact]
    public async Task Test_ModOverwritesGameFiles()
    {
        var loadout = await CreateLoadout();

        // 3D NPC Houses 1.0 https://www.nexusmods.com/stardewvalley/mods/763?tab=files
        var mod = await InstallModFromNexusMods(loadout, ModId.From(763), FileId.From(2874), installer: ServiceProvider.GetRequiredService<DummyInstaller>());

        var diagnostic = await GetSingleDiagnostic(loadout);
        var modOverwritesGameFilesMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.ModOverwritesGameFilesMessageData>>(because: "mod overwrites game files").Which.MessageData;

        modOverwritesGameFilesMessageData.GroupName.Should().Be("TileFile");
        modOverwritesGameFilesMessageData.Group.DataId.Should().Be(mod.LoadoutItemGroupId);

        await VerifyDiagnostic(diagnostic);
    }
}

file class DummyInstaller : ALibraryArchiveInstaller
{
    public DummyInstaller(IServiceProvider serviceProvider, ILogger<DummyInstaller> logger) : base(serviceProvider, logger) { }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        foreach (var fileEntry in libraryArchive.Children)
        {
            var to = new GamePath(LocationId.Game, fileEntry.Path);

            _ = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.AsLibraryFile().FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup,
                    },
                },
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }
}
