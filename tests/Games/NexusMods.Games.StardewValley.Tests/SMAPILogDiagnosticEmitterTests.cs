
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley.Tests;

public class SMAPILogDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<SMAPIGameVersionDiagnosticEmitterTests, StardewValley, SMAPILogDiagnosticEmitter>
{
    private readonly InMemoryFileSystem _fileSystem;
    
    public SMAPILogDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) 
    {
        _fileSystem = new InMemoryFileSystem();
    }

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
        //await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        await ShouldHaveNoDiagnostics(loadout, because: "SMAPI is installed but no log folder is present");

    }

    [Fact]
    public async Task Test_SMAPIErrorLogIsLatest()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        //await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Create log files - THIS DOESNT SEEM TO WORK AND I DON'T KNOW WHY YET
        AbsolutePath appData = _fileSystem.GetKnownPath(KnownPath.ApplicationDataDirectory);
        AbsolutePath errorLogs = appData.Combine(Path.Combine("StardewValley", "ErrorLogs"));
        _fileSystem.CreateDirectory(errorLogs);
        AbsolutePath crashLog = appData.Combine(Path.Combine(Constants.SMAPIErrorFileName));

        var crashFile = crashLog.Create();

        await _fileSystem.WriteAllTextAsync(crashLog, "Crashed");

        _fileSystem.FileExists(crashLog).Should().BeTrue();

        var crashDiagnostic = await GetSingleDiagnostic(loadout);
        var crashDiagnosticMessageData = crashDiagnostic.Should().BeOfType<Diagnostic<Diagnostics.GameRecentlyCrashedMessageData>>(because: "The latest SMAPI log file is a crash log.").Which.MessageData;
        crashDiagnosticMessageData.LogPath.Should().Be(crashLog.ToString(), because: "This is the reported crash log");

        await VerifyDiagnostic(crashDiagnostic);

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
