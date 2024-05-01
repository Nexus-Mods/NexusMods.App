using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using AutoFixture;
using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.DarkestDungeon.Installers;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon.Tests.Installers;

public class NativeModInstallerTests : AModInstallerTest<DarkestDungeon, NativeModInstaller>
{
    public NativeModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Theory]
    [InlineData("")]
    [InlineData("foo/bar/baz/")]
    public async Task Test_GetMods(string basePath)
    {
        var modProjectFile = CreateModProject(out var modProject);
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { $"{basePath}{modProject.Title}/project.xml", modProjectFile },
            { $"{basePath}{modProject.Title}/foo", Array.Empty<byte>() },
        };

        await using var path = await CreateTestArchive(testFiles);

        var (_, modFiles) = await GetModWithFilesFromInstaller(path);
        modFiles.Should().HaveCount(2);
        modFiles.Should().Contain(x => x.To.Path.Equals($"mods/{modProject.Title}/project.xml"));
        modFiles.Should().Contain(x => x.To.Path.Equals($"mods/{modProject.Title}/foo"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // Marvin Seo's Lamia Class Mod 1.03 (https://www.nexusmods.com/darkestdungeon/mods/501)
        var downloadId = await DownloadMod(GameInstallation.Game.Domain, ModId.From(501), FileId.From(2705));
        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        mod.Files.Should().NotBeEmpty();
        mod.Files.Should().AllSatisfy(kv => kv.To.Path.StartsWith("mods/Lamia Mod Base").Should().BeTrue());

    }

    internal static byte[] CreateModProject(out ModProject project)
    {
        var fixture = new Fixture();
        project = fixture.Create<ModProject>();

        using var ms = new MemoryStream();
        new XmlSerializer(typeof(ModProject)).Serialize(ms, project);
        ms.Position = 0;

        return ms.ToArray();
    }
}
