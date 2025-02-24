
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley.Tests;

public class SMAPILogDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<SMAPIGameVersionDiagnosticEmitterTests, StardewValley, SMAPILogDiagnosticEmitter>
{
    public static readonly InMemoryFileSystem _fileSystem = new InMemoryFileSystem();
    
    public SMAPILogDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.15"));
    }

    [Fact]
    public async Task Test_SMAPILogFolderNotPresent()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Create log files

    }

    [Fact]
    public async Task Test_SMAPIErrorLogIsLatest()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Create log files
        
    }

    [Fact]
    public async Task Test_SMAPIErrorLogNotLatest()
    {
        
    }

    [Fact]
    public async Task Test_SMAPIErrorLogValid()
    {

    }

    [Fact]
    public async Task Test_SMAPIErrorLogTooOld()
    {
        
    }
}
