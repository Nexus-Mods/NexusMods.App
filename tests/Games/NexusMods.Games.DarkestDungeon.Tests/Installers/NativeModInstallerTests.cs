using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using AutoFixture;
using FluentAssertions;
using NexusMods.Common;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.DarkestDungeon.Installers;
using NexusMods.Games.DarkestDungeon.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
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
        modFiles.Cast<IToFile>().Should().Contain(x => x.To.Path.Equals($"mods/{modProject.Title}/project.xml"));
        modFiles.Cast<IToFile>().Should().Contain(x => x.To.Path.Equals($"mods/{modProject.Title}/foo"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // Marvin Seo's Lamia Class Mod 1.03 (https://www.nexusmods.com/darkestdungeon/mods/501)
        var (path, hash) = await DownloadMod(GameInstallation.Game.Domain, ModId.From(501), FileId.From(2705));
        await using (path)
        {
            hash.Should().Be(Hash.From(0x34C32E580205FC36));

            var mod = await InstallModFromArchiveIntoLoadout(loadout, path);
            mod.Files.Should().NotBeEmpty();
            mod.Files.Values.Cast<IToFile>().Should().AllSatisfy(kv => kv.To.Path.StartsWith("mods/Lamia Mod Base"));
        }
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
