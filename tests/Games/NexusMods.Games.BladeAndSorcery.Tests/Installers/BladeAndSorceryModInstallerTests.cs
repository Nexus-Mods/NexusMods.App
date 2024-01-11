using FluentAssertions;
using Newtonsoft.Json;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.BladeAndSorcery.Installers;
using NexusMods.Games.BladeAndSorcery.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.BladeAndSorcery.Tests.Installers;

public class BladeAndSorceryModInstallerTests : AModInstallerTest<BladeAndSorcery, BladeAndSorceryModInstaller>
{
    public BladeAndSorceryModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_GetMods()
    {
        var modProject = new ModManifest("ModName", "Desc", "Author", "1.0", "0.12.0.0", "");
        var modProjectFile = CreateModManifest(modProject);
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { "MyMod/manifest.json", modProjectFile },
            { "MyMod/foo", Array.Empty<byte>() },
        };

        await using var path = await CreateTestArchive(testFiles);

        var (mod, modFiles) = await GetModWithFilesFromInstaller(path);

        var toFiles = modFiles
            .Should().HaveCount(2)
            .And.AllBeAssignableTo<IToFile>()
            .Which
            .ToList();

        toFiles.Should().Contain(x => x.To.Path.Equals("BladeAndSorcery_Data/StreamingAssets/Mods/MyMod/manifest.json"));
        toFiles.Should().Contain(x => x.To.Path.Equals("BladeAndSorcery_Data/StreamingAssets/Mods/MyMod/foo"));

        mod.Name.Should().Be("ModName");
        mod.Version.Should().Be("1.0");
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // Dismemberment(U12) 2.4 (https://www.nexusmods.com/bladeandsorcery/mods/1934)
        var downloadId = await DownloadMod(GameInstallation.Game.Domain, ModId.From(1934), FileId.From(21173));
        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        mod.Files.Should().NotBeEmpty();
        mod.Files.Values.Cast<IToFile>().Should().AllSatisfy(kv => kv.To.Path.StartsWith("BladeAndSorcery_Data/StreamingAssets/Mods/Dismemberment").Should().BeTrue());
    }

    internal static byte[] CreateModManifest(ModManifest manifest)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);

        new JsonSerializer().Serialize(writer, manifest);
        writer.Flush();

        ms.Position = 0;

        return ms.ToArray();
    }
}
