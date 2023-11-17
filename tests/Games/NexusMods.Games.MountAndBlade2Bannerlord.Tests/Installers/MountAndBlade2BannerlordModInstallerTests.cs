using System.Text;
using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Installers;

public class MountAndBlade2BannerlordModInstallerTests : AModInstallerTest<MountAndBlade2Bannerlord, MountAndBlade2BannerlordModInstaller>
{
    public MountAndBlade2BannerlordModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithBLSE()
    {
        var loadout = await CreateLoadout();

        // Bannerlord Software Extender (BLSE): Bannerlord Software Extender (BLSE) Main File (Version 1.3.6)
        var downloadId = await DownloadMod(Game.Domain, ModId.From(1), FileId.From(34698));

        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        mod.Files.Should().HaveCount(11);
        mod.Files.Values.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("bin"));
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.BLSE.Shared.dll");
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.BLSE.Standalone.exe");
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.BLSE.Launcher.exe");
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.BLSE.LauncherEx.exe");
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithStandardMod()
    {
        var loadout = await CreateLoadout(false);

        // Harmony: Harmony Main File (Version 2.3.0)
        var downloadId = await DownloadMod(Game.Domain, ModId.From(2006), FileId.From(34666));

        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        mod.Name.Should().BeEquivalentTo("Harmony");
        mod.Version.Should().BeEquivalentTo("v2.3.0.0");
        mod.Files.Values.Cast<IToFile>().Should().HaveCount(49);
        mod.Files.Values.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
        mod.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "SubModule.xml");
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithStandardModMultiple()
    {
        var loadout = await CreateLoadout(false);

        // Calradia at War (Custom Spawns): CalradiaAtWar For Bannerlord v1.1.0 - v1.1.1 - v1.1.2 Main File (Version 1.9.5)
        var downloadId = await DownloadMod(Game.Domain, ModId.From(411), FileId.From(34610));

        var mods = await InstallModsStoredFileIntoLoadout(loadout, downloadId);
        var mod1 = mods.FirstOrDefault(x => x.Name == "Custom Spawns API");
        var mod2 = mods.FirstOrDefault(x => x.Name == "Calradia At War");

        mod1.Name.Should().BeEquivalentTo("Custom Spawns API");
        mod1.Version.Should().BeEquivalentTo("v1.9.5.0");
        mod1.Files.Should().HaveCount(8);
        mod1.Files.Values.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/CustomSpawns"));
        mod1.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "CustomSpawns.dll");
        mod1.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "SubModule.xml");

        mod2.Name.Should().BeEquivalentTo("Calradia At War");
        mod2.Version.Should().BeEquivalentTo("v1.9.1.0");
        mod2.Files.Values.Cast<IToFile>().Should().HaveCount(20);
        mod2.Files.Values.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/CalradiaAtWar"));
        mod2.Files.Values.Cast<IToFile>().Should().Contain(x => x.To.FileName == "SubModule.xml");
    }

    [Fact]
    public async Task Test_WithFakeMod()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            ["Test/SubModule.xml"] = Encoding.UTF8.GetBytes(Data.HarmonySubModuleXml),
            ["Test/bin/Win64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>(),
            ["Test/bin/Gaming.Desktop.x64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>()
        };

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var (mod, modFiles) = await GetModWithFilesFromInstaller(file.Path);
            mod.Name.Should().BeEquivalentTo("Harmony");
            mod.Version.Should().BeEquivalentTo("v2.2.0.0");
            modFiles.Should().HaveCount(3);
            modFiles.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "SubModule.xml");
        }
    }
    [Fact]
    public async Task Test_WithFakeMod_WithModulesRoot()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            ["Modules/Test/SubModule.xml"] = Encoding.UTF8.GetBytes(Data.HarmonySubModuleXml),
            ["Modules/Test/bin/Win64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>(),
            ["Modules/Test/bin/Gaming.Desktop.x64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>()
        };

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var (mod, modFiles) = await GetModWithFilesFromInstaller(file.Path);
            mod.Name.Should().BeEquivalentTo("Harmony");
            mod.Version.Should().BeEquivalentTo("v2.2.0.0");
            modFiles.Should().HaveCount(3);
            modFiles.Cast<IToFile>().Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "SubModule.xml");
        }
    }
}
