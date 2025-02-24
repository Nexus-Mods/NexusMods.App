
namespace NexusMods.Games.StardewValley.Tests;

public class SMAPILogDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<SMAPIGameVersionDiagnosticEmitterTests, StardewValley, SMAPIGameVersionDiagnosticEmitter>
{
    public SMAPILogDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.15"));
    }

    [Fact]
    public async Task Test_SMAPILogNotPresent()
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
